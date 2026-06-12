using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PopUpChaser : MonoBehaviour
{
    enum State { Idle, Aiming, Flying, Recovering, Eating }

    [Header("Chase Settings")]
    public Transform playerTarget;
    public CursorController cursorController;
    public float chaseSpeed = 3f;
    public float acceleration = 5f;
    public float detectionRange = 10f;
    public float aimRange = 6f;

    [Header("Arrow Attack")]
    public float aimDuration = 0.5f;
    public float flySpeed = 8f;
    public float homingCutoff = 1.5f;
    public float homingStrength = 3f;
    public float missCooldown = 2f;

    [Header("Sticker Eating")]
    public float eatDuration = 1.5f;
    public LayerMask stickerLayer;

    [Header("Cursor Effects")]
    public float glitchDuration = 1.5f;

    [Header("Visual")]
    public Color alertColor = Color.red;
    public Color aimColor = Color.yellow;
    public float pulseSpeed = 4f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color originalColor;
    private Vector2 velocity = Vector2.zero;
    private float pulseTimer;
    private State state = State.Idle;

    // Arrow attack
    private float aimTimer = 0f;
    private Vector2 flyTarget;
    private float recoverTimer = 0f;

    // Sticker eating
    private float eatTimer = 0f;
    private Sticker currentSticker = null;

    // Vulnerability grace
    private float vulnerableTimer = 0f;

    // Spawn
    private Vector3 spawnPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalColor = sr.color;
        spawnPosition = transform.position;

        Collider2D existing = GetComponent<Collider2D>();
        float radius = 0.5f;
        if (existing is BoxCollider2D box)
        {
            radius = Mathf.Max(box.size.x, box.size.y) * 0.5f;
            Destroy(existing);
        }
        CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
        circle.radius = radius;
        circle.isTrigger = true;

        if (playerTarget == null)
            playerTarget = FindFirstObjectByType<PlayerMovement>()?.transform;
    }

    void Update()
    {
        if (playerTarget == null) return;

        if (vulnerableTimer > 0f)
            vulnerableTimer -= Time.deltaTime;

        if (state == State.Eating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f) FinishEating();
            return;
        }

        if (state == State.Recovering)
        {
            recoverTimer -= Time.deltaTime;
            if (recoverTimer <= 0f)
            {
                vulnerableTimer = 0.3f;
                EnterIdle();
            }
            return;
        }

        if (state == State.Aiming)
        {
            aimTimer -= Time.deltaTime;
            float flash = Mathf.PingPong(Time.time * 20f, 1f);
            sr.color = Color.Lerp(originalColor, aimColor, flash);

            if (aimTimer <= 0f)
            {
                flyTarget = GetFlyTarget();
                EnterFlying();
            }
            return;
        }

        if (state == State.Flying)
        {
            float distToDest = Vector2.Distance(transform.position, flyTarget);
            if (distToDest < 0.5f || distToDest > 50f)
            {
                EnterRecovering();
                return;
            }
            return;
        }

        // Idle state
        float dist = Vector2.Distance(transform.position, playerTarget.position);
        if (dist < detectionRange)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float t = Mathf.Sin(pulseTimer) * 0.5f + 0.5f;
            sr.color = Color.Lerp(originalColor, alertColor, t);
        }
        else
        {
            sr.color = originalColor;
            pulseTimer = 0f;
        }

        Sticker sticker = FindClosestSticker();
        if (canAttack() && dist < aimRange && sticker == null)
            EnterAiming();
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;
        if (state == State.Eating || state == State.Recovering) return;
        if (state == State.Aiming) return;

        if (state == State.Flying)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distToPlayer > homingCutoff)
                flyTarget = Vector2.MoveTowards(flyTarget, playerTarget.position, homingStrength * Time.fixedDeltaTime);

            Vector2 dir = (flyTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * flySpeed;
            return;
        }

        Vector2 targetPos = GetTargetPosition();
        Vector2 currentPos = transform.position;
        float dist = Vector2.Distance(currentPos, targetPos);

        if (dist < detectionRange && dist > 0.1f)
        {
            Vector2 desired = (targetPos - currentPos).normalized * chaseSpeed;
            velocity = Vector2.MoveTowards(velocity, desired, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = velocity;
    }

    void LateUpdate()
    {
        if (state == State.Flying)
        {
            Vector2 dir = rb.linearVelocity;
            if (dir.sqrMagnitude > 0.01f)
                transform.up = dir.normalized;
        }
        else if (state == State.Aiming && playerTarget != null)
        {
            Vector2 dir = (GetFlyTarget() - transform.position).normalized;
            transform.up = Vector3.RotateTowards(transform.up, dir, 720f * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }
        else if (state == State.Idle)
        {
            transform.up = Vector3.RotateTowards(transform.up, Vector3.up, 360f * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }
    }

    bool canAttack()
    {
        return state == State.Idle && vulnerableTimer <= 0f;
    }

    Vector2 GetFlyTarget()
    {
        if (cursorController != null && cursorController.isCursorMode)
            return cursorController.transform.position;

        return playerTarget.position + Vector3.up * 0.5f;
    }

    Vector2 GetTargetPosition()
    {
        Sticker sticker = FindClosestSticker();
        if (sticker != null)
            return sticker.transform.position;

        if (cursorController != null && cursorController.isCursorMode)
            return cursorController.transform.position;

        Vector2 playerPos = playerTarget.position;
        playerPos.y += 0.5f;
        return playerPos;
    }

    void EnterIdle()
    {
        state = State.Idle;
        sr.color = originalColor;
    }

    void EnterAiming()
    {
        state = State.Aiming;
        aimTimer = aimDuration;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    void EnterFlying()
    {
        state = State.Flying;
        sr.color = originalColor;
    }

    void EnterRecovering()
    {
        state = State.Recovering;
        recoverTimer = missCooldown;
        sr.color = originalColor * 0.5f;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    Sticker FindClosestSticker()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, stickerLayer);
        if (hits.Length == 0) return null;

        Collider2D closest = null;
        float minDist = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            Sticker s = hit.GetComponent<Sticker>();
            if (s == null) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < minDist) { minDist = d; closest = hit; }
        }
        return closest?.GetComponent<Sticker>();
    }

    void StartEating(Sticker sticker)
    {
        state = State.Eating;
        eatTimer = eatDuration;
        currentSticker = sticker;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        sticker.SetIntangible();
    }

    void FinishEating()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
        currentSticker = null;
        EnterRecovering();
    }

    public void ResetToSpawn()
    {
        state = State.Idle;
        currentSticker = null;
        vulnerableTimer = 0f;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        sr.color = originalColor;
        transform.up = Vector3.up;
        transform.position = spawnPosition;
    }

    public void Die()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state == State.Idle)
        {
            Sticker sticker = other.GetComponent<Sticker>();
            if (sticker != null)
            {
                StartEating(sticker);
                return;
            }
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Rigidbody2D playerRb = other.attachedRigidbody;
            bool canStomp = state == State.Eating || state == State.Recovering || vulnerableTimer > 0f;
            bool isStomping = canStomp && playerRb.linearVelocity.y < 0
                && other.transform.position.y > transform.position.y + 0.3f;

            if (isStomping)
            {
                Die();
                player.Bounce();
            }
            else
            {
                player.TakeDamage(transform.position);

                if (cursorController != null && cursorController.isCursorMode)
                {
                    cursorController.Glitch(glitchDuration);
                    FindFirstObjectByType<StickerSpawner>()?.ForceCooldown();
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, aimRange);
    }
}
