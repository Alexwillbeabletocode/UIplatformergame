using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Sticker : MonoBehaviour
{
    [Header("Timer")]
    public float lifetime = 3f;

    public StickerSpawner spawner { get; set; }

    private float timer;
    private Button targetButton;
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        timer = lifetime;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero);
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
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer <= 0f)
            Expire();
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
}
