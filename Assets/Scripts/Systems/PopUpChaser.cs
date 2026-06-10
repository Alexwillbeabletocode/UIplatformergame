using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PopUpChaser : MonoBehaviour
{
    enum State { Chasing, Eating, Stunned }

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
    public float agroRange = 2.5f;
    public float catchRadius = 0.8f;
    public float windupDuration = 0.15f;
    public float lungeSpeed = 10f;
    public float lungeMaxDistance = 3f;
    public float catchCooldown = 2f;
    public float stunDuration = 1.5f;
    public float stunRecoilForce = 5f;
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

    // Cursor lunge
    private float lungeCooldown = 0f;
    private float windupTimer = 0f;
    private bool isWindingUp = false;
    private bool isLunging = false;
    private Vector3 lungeTarget;
    private float lungeTimer = 0f;

    // Stun
    private float stunTimer = 0f;

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

        // Timers
        if (lungeCooldown > 0f) lungeCooldown -= Time.deltaTime;

        if (state == State.Eating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f) FinishEating();
            return;
        }

        if (state == State.Stunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) state = State.Chasing;
            return;
        }

        // Windup telegraph
        if (isWindingUp)
        {
            windupTimer -= Time.deltaTime;
            float flash = Mathf.PingPong(Time.time * 30f, 1f);
            sr.color = Color.Lerp(originalColor, Color.white, flash);

            if (windupTimer <= 0f)
                StartLunge();

            return;
        }

        // Lunge in progress
        if (isLunging)
        {
            lungeTimer -= Time.deltaTime;
            float distToTarget = Vector2.Distance(transform.position, lungeTarget);

            if (distToTarget < catchRadius)
            {
                // Arrived — check if cursor is still close enough to catch
                float cursorDist = Vector2.Distance(transform.position, cursorController.transform.position);
                if (cursorDist < catchRadius * 1.5f)
                    DoCatch();
                else
                    CancelLunge();
            }

            if (lungeTimer <= 0f)
                CancelLunge();

            return;
        }

        // Pulse when player is in range
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

        // Start windup when cursor is in agro range
        if (lungeCooldown <= 0f && cursorController != null && cursorController.isCursorMode)
        {
            float cursorDist = Vector2.Distance(transform.position, cursorController.transform.position);
            if (cursorDist < agroRange)
                StartWindup();
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;
        if (state == State.Eating || state == State.Stunned) return;
        if (isWindingUp) return;

        if (isLunging)
        {
            Vector2 dir = (lungeTarget - transform.position).normalized;
            rb.linearVelocity = dir * lungeSpeed;
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

    void StartWindup()
    {
        isWindingUp = true;
        windupTimer = windupDuration;
        lungeTarget = cursorController.transform.position;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    void StartLunge()
    {
        isWindingUp = false;
        isLunging = true;

        Vector3 dir = (lungeTarget - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, lungeTarget);
        if (dist > lungeMaxDistance)
        {
            lungeTarget = transform.position + dir * lungeMaxDistance;
            dist = lungeMaxDistance;
        }

        lungeTimer = dist / lungeSpeed + 0.2f;
    }

    void CancelLunge()
    {
        isLunging = false;
        lungeCooldown = catchCooldown * 0.3f;
        sr.color = originalColor;
    }

    void DoCatch()
    {
        isLunging = false;
        lungeCooldown = catchCooldown;

        cursorController.Glitch(glitchDuration);
        if (stickerSpawner != null) stickerSpawner.ForceCooldown();

        // Recoil
        Vector3 recoilDir = (transform.position - cursorController.transform.position).normalized;
        velocity = (Vector2)recoilDir * stunRecoilForce;
        rb.linearVelocity = velocity;

        sr.color = originalColor;
        state = State.Stunned;
        stunTimer = stunDuration;
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
    }

    void FinishEating()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
        currentSticker = null;
        state = State.Chasing;
    }

    public void Die()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
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
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, agroRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}
