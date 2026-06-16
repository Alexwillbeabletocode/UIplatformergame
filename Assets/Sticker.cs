using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Sticker : MonoBehaviour
{
    [Header("Timer")]
    public float lifetime = 3f;

    public StickerSpawner spawner { get; set; }

    private float timer;
    private Keyhole targetKeyhole;
    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        timer = lifetime;

        col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            if (sr.sprite != null)
                col.size = sr.sprite.bounds.size;
        }

        if (targetKeyhole != null)
        {
            targetKeyhole.HoldActivate();
        }
        else
        {
            int stickerLayer = 1 << LayerMask.NameToLayer("Sticker");
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero, 100f, ~stickerLayer);
            if (hit.collider != null)
            {
                targetKeyhole = hit.collider.GetComponent<Keyhole>();
                if (targetKeyhole != null)
                    targetKeyhole.HoldActivate();
            }
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

    public void SetIntangible()
    {
        col.isTrigger = true;
    }

    void Expire()
    {
        ReleaseTargets();
        spawner?.OnStickerDestroyed();
        Destroy(gameObject);
    }

    public void DestroyByChaser()
    {
        ReleaseTargets();
        spawner?.OnStickerDestroyed();
        Destroy(gameObject);
    }

    void ReleaseTargets()
    {
        if (targetKeyhole != null)
            targetKeyhole.HoldRelease();
    }

    public Keyhole AssignedKeyhole
    {
        set { targetKeyhole = value; }
    }
}
