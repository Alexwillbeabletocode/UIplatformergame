using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Acceleration")]
    public float groundAcceleration = 50f;  // How fast you reach full speed on the ground
    public float groundDeceleration = 60f;  // How fast you stop on the ground
    public float airAcceleration = 25f;     // Slower acceleration in the air
    public float airDeceleration = 15f;     // Less deceleration in the air (floatier stop)

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask extraGroundLayers;     // Add draggable platform layer here

    [Header("Wall Check")]
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckRadius = 0.15f;
    public LayerMask wallLayer;

    [Header("Wall Slide")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 2f;       // Max fall speed while sliding down a wall

    [Header("Lives & Damage")]
    public int lives = 3;
    public float invincibilityDuration = 1f;

    [Header("Better Jump Feel")]
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    private Rigidbody2D rb;
    private float moveInput;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;
    private bool isWallSliding;
    private bool isInvincible;
    private float invincibilityTimer;
    private SpriteRenderer playerSr;

    public CursorController cursorController;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (playerSr != null)
                playerSr.enabled = Mathf.FloorToInt(invincibilityTimer * 10f) % 2 == 0;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                if (playerSr != null) playerSr.enabled = true;
            }
        }

        if (cursorController != null && cursorController.isCursorMode)
        {
            moveInput = 0f;
            // Don't return � still need gravity to apply, just no input
            // Horizontal deceleration is handled in FixedUpdate
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground check � combines groundLayer with any extra layers (e.g. draggable platforms)
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer | extraGroundLayers);

        // Wall checks
        isTouchingWallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        isTouchingWallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        // Wall sliding: in the air, pressing into a wall
        isWallSliding = enableWallSlide
            && !isGrounded
            && ((isTouchingWallLeft && moveInput < 0) || (isTouchingWallRight && moveInput > 0));

        // Coyote time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        // Jump
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
        }

        // Short hop
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // Gravity
        if (isWallSliding)
        {
            rb.gravityScale = 1f; // Normal gravity while on a wall (wall slide caps fall speed below)
        }
        else if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    void FixedUpdate()
    {
        bool inCursorMode = cursorController != null && cursorController.isCursorMode;

        // Target horizontal speed
        bool pushingIntoWall = (moveInput < 0 && isTouchingWallLeft) || (moveInput > 0 && isTouchingWallRight);
        float targetSpeed = (!inCursorMode && !pushingIntoWall) ? moveInput * moveSpeed : 0f;

        // Pick the right acceleration rate
        float accel;
        if (inCursorMode)
        {
            accel = groundDeceleration; // Smooth stop when switching modes
        }
        else if (isGrounded)
        {
            accel = Mathf.Abs(moveInput) > 0.01f ? groundAcceleration : groundDeceleration;
        }
        else
        {
            accel = Mathf.Abs(moveInput) > 0.01f ? airAcceleration : airDeceleration;
        }

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);

        // Wall slide: cap downward speed so the player doesn't rocket down the wall
        float newY = rb.linearVelocity.y;
        if (isWallSliding && newY < -wallSlideSpeed)
            newY = -wallSlideSpeed;

        rb.linearVelocity = new Vector2(newX, newY);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
    }

    public void Bounce()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.6f);
    }

    public void Knockback(Vector2 source, float force, float upwardForce)
    {
        Vector2 dir = (rb.position - source).normalized;
        rb.linearVelocity = new Vector2(dir.x * force, upwardForce);
        coyoteTimeCounter = 0f;
    }

    public void TakeDamage(Vector2 source)
    {
        if (isInvincible) return;
        Knockback(source, 12f, 5f);
        Die();
    }

    public void Die()
    {
        if (lives <= 0) return;

        lives--;
        if (lives <= 0)
        {
            lives = 3;
            RespawnManager.Instance.RespawnPlayer();
            RespawnManager.Instance.ResetChasers();
            enabled = true;
            return;
        }

        enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1f;
        Invoke(nameof(Respawn), RespawnManager.Instance.deathDelay);
    }

    void Respawn()
    {
        RespawnManager.Instance.RespawnPlayer();
        RespawnManager.Instance.ResetChasers();
        enabled = true;
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
    }

    void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.color = isTouchingWallLeft ? Color.blue : Color.yellow;
        Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
        Gizmos.color = isTouchingWallRight ? Color.blue : Color.yellow;
        Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
    }
}