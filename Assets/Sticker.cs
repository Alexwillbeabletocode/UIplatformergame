using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Sticker : MonoBehaviour
{
    [Header("Timer")]
    public float lifetime = 3f;

    public StickerSpawner spawner { get; set; }

    [Header("Platform Mode")]
    public float expandRange = 2f;
    public float expandScale = 2.5f;
    public float expandAnimSpeed = 6f;
    public Color inRangeColor = Color.green;

    private float timer;
    private Button targetButton;
    private SpriteRenderer sr;
    private Color originalColor;
    private Collider2D col;
    private Transform playerTransform;
    private Vector3 originalScale;
    private bool isExpanded = false;
    private float currentScale = 1f;
    private float targetScale = 1f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalColor = sr.color;
        originalScale = transform.localScale;
        timer = lifetime;
        currentScale = 1f;
        targetScale = 1f;

        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;

        // Ignore Sticker layer so we don't raycast into ourselves
        int stickerLayer = 1 << LayerMask.NameToLayer("Sticker");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero, 100f, ~stickerLayer);
        if (hit.collider != null)
        {
            targetButton = hit.collider.GetComponent<Button>();
            if (targetButton != null)
                targetButton.HoldActivate();
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(timer / lifetime);

        // Scale animation
        currentScale = Mathf.MoveTowards(currentScale, targetScale, expandAnimSpeed * Time.deltaTime);
        transform.localScale = originalScale * currentScale;

        // E key toggle
        if (playerTransform != null && Input.GetKeyDown(KeyCode.E))
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < expandRange)
            {
                if (isExpanded) Collapse();
                else Expand();
            }
        }

        // Color: green hint when in range, otherwise normal
        Color baseColor = originalColor;
        if (playerTransform != null && !isExpanded)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < expandRange)
                baseColor = Color.Lerp(originalColor, inRangeColor, 0.5f);
        }
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        if (timer <= 0f)
            Expire();
    }

    void Expand()
    {
        isExpanded = true;
        targetScale = expandScale;
        col.isTrigger = false;

        // Release the button — trade-off: hold button OR be a platform
        if (targetButton != null)
            targetButton.HoldRelease();
    }

    void Collapse()
    {
        isExpanded = false;
        targetScale = 1f;
        col.isTrigger = true;

        // Re-activate the button
        if (targetButton != null)
            targetButton.HoldActivate();
    }

    // Called by chaser when eating starts — make intangible without reactivating button
    public void SetIntangible()
    {
        col.isTrigger = true;
    }

    void Expire()
    {
        ReleaseButton();
        spawner?.OnStickerDestroyed();
        Destroy(gameObject);
    }

    public void DestroyByChaser()
    {
        ReleaseButton();
        spawner?.OnStickerDestroyed();
        Destroy(gameObject);
    }

    void ReleaseButton()
    {
        if (targetButton != null)
            targetButton.HoldRelease();
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, expandRange);
        }
    }
}
