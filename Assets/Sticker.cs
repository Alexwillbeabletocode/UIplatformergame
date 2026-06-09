using UnityEngine;

// Attach to a Sticker prefab (needs a SpriteRenderer).
// Spawned by StickerSpawner when right-clicking in Cursor Mode.
// Raycasts for a Button underneath, holds it active, then expires.

[RequireComponent(typeof(SpriteRenderer))]
public class Sticker : MonoBehaviour
{
    [Header("Timer")]
    public float lifetime = 3f;

    private float timer;
    private Button targetButton;   // The button this sticker is holding
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        timer = lifetime;

        // Check what's directly under the sticker on placement
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

        // Fade out as the sticker runs out of time
        float alpha = Mathf.Clamp01(timer / lifetime);
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (timer <= 0f)
            Expire();
    }

    void Expire()
    {
        if (targetButton != null)
            targetButton.HoldRelease();

        Destroy(gameObject);
    }
}