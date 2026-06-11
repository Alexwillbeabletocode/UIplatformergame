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
    private Sprite triangleSprite;
    private Sprite rectangleSprite;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalColor = sr.color;
        originalScale = transform.localScale;
        timer = lifetime;
        currentScale = 1f;
        targetScale = 1f;
        triangleSprite = sr.sprite;
        rectangleSprite = CreateRectangleSprite();

        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;

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

        currentScale = Mathf.MoveTowards(currentScale, targetScale, expandAnimSpeed * Time.deltaTime);
        transform.localScale = originalScale * currentScale;

        if (playerTransform != null && Input.GetKeyDown(KeyCode.E))
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < expandRange)
            {
                if (isExpanded) Collapse();
                else Expand();
            }
        }

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
        sr.sprite = rectangleSprite;

        // Swap collider to solid with physics refresh
        col.enabled = false;
        col.isTrigger = false;
        col.enabled = true;

        if (targetButton != null)
            targetButton.HoldRelease();
    }

    void Collapse()
    {
        isExpanded = false;
        targetScale = 1f;
        sr.sprite = triangleSprite;

        col.enabled = false;
        col.isTrigger = true;
        col.enabled = true;

        if (targetButton != null)
            targetButton.HoldActivate();
    }

    public void SetIntangible()
    {
        col.enabled = false;
        col.isTrigger = true;
        col.enabled = true;
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

    Sprite CreateRectangleSprite()
    {
        Texture2D tex = new Texture2D(64, 32);
        Color fill = Color.white;
        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, fill);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 32);
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
