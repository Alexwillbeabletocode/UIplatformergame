using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PopUpChaser : MonoBehaviour
{
    enum State { Chasing, Eating }

    [Header("Chase Settings")]
    public Transform playerTarget;
    public CursorController cursorController;
    public StickerSpawner stickerSpawner;
    public float chaseSpeed = 3f;
    public float acceleration = 5f;
    public float detectionRange = 10f;
    public float hoverHeight = 1f;

    [Header("Damage")]
    public float knockbackForce = 12f;
    public float knockbackUpward = 5f;

    [Header("Sticker Eating")]
    public float eatDuration = 0.5f;
    public LayerMask stickerLayer;

    [Header("Cursor Catch")]
    public float cursorCatchRadius = 0.5f;
    public float glitchDuration = 1.5f;

    [Header("Visual")]
    public Color alertColor = Color.red;
    public float pulseSpeed = 4f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color originalColor;
    private Vector2 velocity = Vector2.zero;
    private float pulseTimer;
    private State state = State.Chasing;
    private float eatTimer = 0f;
    private Sticker currentSticker = null;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalColor = sr.color;

        if (playerTarget == null)
            playerTarget = FindFirstObjectByType<PlayerMovement>()?.transform;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (playerTarget == null) return;

        if (state == State.Eating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f)
                FinishEating();
            return;
        }

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

        // Check cursor proximity
        if (cursorController != null && cursorController.isCursorMode)
        {
            float cursorDist = Vector2.Distance(transform.position, cursorController.transform.position);
            if (cursorDist < cursorCatchRadius)
                CatchCursor();
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null || state == State.Eating)
            return;

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

    Vector2 GetTargetPosition()
    {
        Sticker sticker = FindClosestSticker();
        if (sticker != null)
            return sticker.transform.position;

        if (cursorController != null && cursorController.isCursorMode)
            return cursorController.transform.position;

        Vector2 playerPos = playerTarget.position;
        playerPos.y += hoverHeight;
        return playerPos;
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
            if (d < minDist)
            {
                minDist = d;
                closest = hit;
            }
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
    }

    void FinishEating()
    {
        if (currentSticker != null)
            currentSticker.DestroyByChaser();

        currentSticker = null;
        state = State.Chasing;
    }

    void CatchCursor()
    {
        cursorController.Glitch(glitchDuration);
        if (stickerSpawner != null)
            stickerSpawner.ForceCooldown();
    }

    public void Die()
    {
        if (currentSticker != null)
            currentSticker.DestroyByChaser();

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Sticker sticker = other.GetComponent<Sticker>();
        if (sticker != null && state == State.Chasing)
        {
            StartEating(sticker);
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Rigidbody2D playerRb = other.attachedRigidbody;
            bool isStomping = playerRb.linearVelocity.y < 0
                && other.transform.position.y > transform.position.y + 0.8f;

            if (isStomping)
            {
                Die();
                player.Bounce();
            }
            else
            {
                player.Knockback(transform.position, knockbackForce, knockbackUpward);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, cursorCatchRadius);
    }
}
