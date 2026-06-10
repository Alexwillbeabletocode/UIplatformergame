using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PopUpChaser : MonoBehaviour
{
    [Header("Chase Settings")]
    public Transform playerTarget;
    public float chaseSpeed = 3f;
    public float acceleration = 5f;
    public float detectionRange = 10f;
    public float hoverHeight = 1f;

    [Header("Damage")]
    public float knockbackForce = 12f;
    public float knockbackUpward = 5f;

    [Header("Visual")]
    public Color alertColor = Color.red;
    public float pulseSpeed = 4f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Color originalColor;
    private Vector2 velocity = Vector2.zero;
    private float pulseTimer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalColor = sr.color;

        if (playerTarget == null)
            playerTarget = FindFirstObjectByType<PlayerMovement>()?.transform;
    }

    void Update()
    {
        if (playerTarget == null) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        if (dist < detectionRange)
        {
            // Pulse color when chasing
            pulseTimer += Time.deltaTime * pulseSpeed;
            float t = Mathf.Sin(pulseTimer) * 0.5f + 0.5f;
            sr.color = Color.Lerp(originalColor, alertColor, t);
        }
        else
        {
            sr.color = originalColor;
            pulseTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;

        Vector2 targetPos = playerTarget.position;
        targetPos.y += hoverHeight;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.Knockback(transform.position, knockbackForce, knockbackUpward);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
