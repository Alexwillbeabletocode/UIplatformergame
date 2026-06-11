using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PopUpChaser : MonoBehaviour
{
    enum State { Idle, Aiming, Flying, Recovering, Eating, Stunned }

    [Header("Chase Settings")]
    public Transform playerTarget;
    public CursorController cursorController;
    public StickerSpawner stickerSpawner;
    public float chaseSpeed = 3f;
    public float acceleration = 5f;
    public float detectionRange = 10f;
    public float aimRange = 6f;

    [Header("Arrow Attack")]
    public float aimDuration = 0.5f;
    public float flySpeed = 8f;
    public float homingCutoff = 1.5f;
    public float homingStrength = 3f;
    public float missCooldown = 1f;
    public float dazeDuration = 0.3f;

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
    public Color aimColor = Color.yellow;
    public float pulseSpeed = 4f;

    private SpriteRenderer sr;
    private SpriteRenderer rootSr;
    private Transform visualPivot;
    private Rigidbody2D rb;
    private Color originalColor;
    private Vector2 velocity = Vector2.zero;
    private float pulseTimer;
    private State state = State.Idle;

    // Arrow attack
    private float aimTimer = 0f;
    private Vector2 flyTarget;
    private float recoverTimer = 0f;
    private bool isDazed = false;
    private bool canAttack = true;
    private float attackCooldownTimer = 0f;

    // Sticker eating
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

    // Spawn
    private Vector3 spawnPosition;

    void Start()
    {
        rootSr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;

        // Create visual child that rotates independently (collider stays upright on root)
        GameObject visualGO = new GameObject("Visual");
        visualGO.transform.SetParent(transform, false);
        visualGO.transform.localPosition = Vector3.zero;
        SpriteRenderer visualSr = visualGO.AddComponent<SpriteRenderer>();
        visualSr.sprite = rootSr.sprite;
        visualSr.sortingLayerID = rootSr.sortingLayerID;
        visualSr.sortingOrder = rootSr.sortingOrder;
        visualSr.color = rootSr.color;
        visualPivot = visualGO.transform;

        // Use visual child for all rendering; hide root sprite
        sr = visualSr;
        originalColor = rootSr.color;
        rootSr.enabled = false;

        if (playerTarget == null)
            playerTarget = FindFirstObjectByType<PlayerMovement>()?.transform;

        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (playerTarget == null) return;

        if (lungeCooldown > 0f) lungeCooldown -= Time.deltaTime;
        if (!canAttack) attackCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer <= 0f) canAttack = true;

        if (state == State.Eating)
        {
            eatTimer -= Time.deltaTime;
            if (eatTimer <= 0f) FinishEating();
            return;
        }

        if (state == State.Stunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) EnterIdle();
            return;
        }

        if (isWindingUp)
        {
            windupTimer -= Time.deltaTime;
            float flash = Mathf.PingPong(Time.time * 30f, 1f);
            sr.color = Color.Lerp(originalColor, Color.white, flash);
            if (windupTimer <= 0f) StartLunge();
            return;
        }

        if (isLunging || state == State.Flying)
        {
            // Cursor lunge tick
            if (isLunging)
            {
                lungeTimer -= Time.deltaTime;
                float distToTarget = Vector2.Distance(transform.position, lungeTarget);
                if (distToTarget < catchRadius)
                {
                    float cursorDist = Vector2.Distance(transform.position, cursorController.transform.position);
                    if (cursorDist < catchRadius * 1.5f) DoCatch();
                    else CancelLunge();
                }
                if (lungeTimer <= 0f) CancelLunge();
                return;
            }

            // Arrow fly tick
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
        }

        if (state == State.Aiming)
        {
            aimTimer -= Time.deltaTime;
            float flash = Mathf.PingPong(Time.time * 20f, 1f);
            sr.color = Color.Lerp(originalColor, aimColor, flash);

            if (aimTimer <= 0f)
            {
                flyTarget = playerTarget.position;
                EnterFlying();
            }
            return;
        }

        if (state == State.Recovering)
        {
            recoverTimer -= Time.deltaTime;
            if (isDazed && recoverTimer <= missCooldown - dazeDuration)
            {
                isDazed = false;
                sr.color = originalColor;
            }
            if (recoverTimer <= 0f) EnterIdle();
            return;
        }

        // Idle state
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
            {
                StartWindup();
                return;
            }
        }

        // Start aiming if player is in range (sticker takes priority in FixedUpdate movement)
        Sticker sticker = FindClosestSticker();
        if (canAttack && dist < aimRange && sticker == null)
            EnterAiming();
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;
        if (state == State.Eating || state == State.Stunned) return;
        if (isWindingUp) return;
        if (state == State.Aiming || (state == State.Recovering && isDazed)) return;

        if (isLunging)
        {
            Vector2 dir = (lungeTarget - transform.position).normalized;
            rb.linearVelocity = dir * lungeSpeed;
            return;
        }

        if (state == State.Flying)
        {
            // Slight tracking toward player until close, then straight
            float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distToPlayer > homingCutoff)
            {
                Vector2 toPlayer = (Vector2)playerTarget.position - (Vector2)transform.position;
                flyTarget = Vector2.MoveTowards(flyTarget, playerTarget.position, homingStrength * Time.fixedDeltaTime);
            }

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
        if (state == State.Flying || isLunging)
        {
            Vector2 dir = rb.linearVelocity;
            if (dir.sqrMagnitude > 0.01f)
                visualPivot.up = dir.normalized;
        }
        else if (state == State.Aiming && playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            visualPivot.up = Vector3.RotateTowards(visualPivot.up, dir, 720f * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }
        else if (state == State.Idle)
        {
            visualPivot.up = Vector3.RotateTowards(visualPivot.up, Vector3.up, 360f * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }
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
        canAttack = false;
        attackCooldownTimer = missCooldown;
        sr.color = originalColor;
    }

    void EnterRecovering()
    {
        state = State.Recovering;
        recoverTimer = missCooldown;
        isDazed = true;
        sr.color = originalColor * 0.5f;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
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
        EnterIdle();
    }

    void DoCatch()
    {
        isLunging = false;
        lungeCooldown = catchCooldown;

        cursorController.Glitch(glitchDuration);
        if (stickerSpawner != null) stickerSpawner.ForceCooldown();

        Vector3 recoilDir = (transform.position - cursorController.transform.position).normalized;
        velocity = (Vector2)recoilDir * stunRecoilForce;
        rb.linearVelocity = velocity;

        sr.color = originalColor;
        state = State.Stunned;
        stunTimer = stunDuration;
    }

    Vector2 GetTargetPosition()
    {
        // Sticker highest priority
        Sticker sticker = FindClosestSticker();
        if (sticker != null)
            return sticker.transform.position;

        if (cursorController != null && cursorController.isCursorMode)
            return cursorController.transform.position;

        Vector2 playerPos = playerTarget.position;
        playerPos.y += 0.5f;
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

        sticker.SetIntangible();
    }

    void FinishEating()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
        currentSticker = null;
        EnterIdle();
    }

    public void ResetToSpawn()
    {
        isWindingUp = false;
        isLunging = false;
        state = State.Idle;
        currentSticker = null;
        canAttack = true;
        attackCooldownTimer = 0f;
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        sr.color = originalColor;
        visualPivot.up = Vector3.up;
        transform.position = spawnPosition;
    }

    public void Die()
    {
        if (currentSticker != null) currentSticker.DestroyByChaser();
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Sticker eating only in Idle state
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
            bool isStomping = playerRb.linearVelocity.y < 0
                && other.transform.position.y > transform.position.y + 0.8f;

            if (isStomping)
            {
                Die();
                player.Bounce();
            }
            else
            {
                player.TakeDamage(transform.position);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, aimRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, agroRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}
