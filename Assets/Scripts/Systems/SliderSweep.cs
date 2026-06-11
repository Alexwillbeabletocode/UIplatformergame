using UnityEngine;

public class SliderSweep : MonoBehaviour
{
    public float trackLength = 6f;
    public float thumbSize = 0.8f;
    public float speed = 2f;
    public float pauseDuration = 0.5f;

    public Color trackColor = new Color(0.3f, 0.3f, 0.3f);
    public Color thumbColor = new Color(0.8f, 0.2f, 0.2f);
    public Color thumbActiveColor = new Color(1f, 0.4f, 0.1f);

    private GameObject trackObj;
    private GameObject thumbObj;
    private SpriteRenderer trackSr;
    private SpriteRenderer thumbSr;
    private BoxCollider2D trackCollider;
    private BoxCollider2D thumbCollider;
    private PlayerMovement player;

    private float direction = 1f;
    private float thumbX;
    private float leftBound;
    private float rightBound;
    private bool paused;
    private float pauseTimer;

    void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>();

        // Track
        trackObj = new GameObject("Track");
        trackObj.transform.SetParent(transform, false);
        trackObj.layer = gameObject.layer;
        trackObj.transform.localPosition = Vector3.zero;

        trackSr = trackObj.AddComponent<SpriteRenderer>();
        trackSr.sprite = CreateTrackSprite();
        trackSr.sortingOrder = 5;

        trackCollider = trackObj.AddComponent<BoxCollider2D>();
        trackCollider.size = new Vector2(trackLength, 0.3f);
        trackCollider.isTrigger = false;

        // Thumb
        thumbObj = new GameObject("Thumb");
        thumbObj.transform.SetParent(transform, false);
        thumbObj.layer = gameObject.layer;

        thumbSr = thumbObj.AddComponent<SpriteRenderer>();
        thumbSr.sprite = CreateThumbSprite();
        thumbSr.sortingOrder = 6;

        thumbCollider = thumbObj.AddComponent<BoxCollider2D>();
        thumbCollider.size = Vector2.one * thumbSize;
        thumbCollider.isTrigger = true;

        leftBound = -trackLength / 2f + thumbSize / 2f;
        rightBound = trackLength / 2f - thumbSize / 2f;
        thumbX = leftBound;
        thumbObj.transform.localPosition = new Vector3(thumbX, 0.75f, 0f);

        UpdateTrackScale();
    }

    void Update()
    {
        if (paused)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseDuration)
            {
                paused = false;
                direction *= -1f;
            }
            return;
        }

        thumbX += direction * speed * Time.deltaTime;

        if (thumbX >= rightBound)
        {
            thumbX = rightBound;
            paused = true;
            pauseTimer = 0f;
        }
        else if (thumbX <= leftBound)
        {
            thumbX = leftBound;
            paused = true;
            pauseTimer = 0f;
        }

        thumbObj.transform.localPosition = new Vector3(thumbX, 0.75f, 0f);
        thumbSr.color = Color.Lerp(thumbColor, thumbActiveColor, Mathf.PingPong(Time.time * 3f, 1f));
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (player == null) return;

        // Push player in direction of thumb movement
        Vector3 pushDir = new Vector3(direction, 0.5f, 0f).normalized;
        player.Knockback(thumbObj.transform.position, 8f, 4f);
    }

    void UpdateTrackScale()
    {
        trackObj.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    Sprite CreateTrackSprite()
    {
        int w = 64, h = 16;
        Texture2D tex = new Texture2D(w, h);
        Color trackDark = new Color(0.2f, 0.2f, 0.2f);
        Color tickColor = new Color(0.5f, 0.5f, 0.5f);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (y == 0 || y == h - 1 || y == h / 2)
                    tex.SetPixel(x, y, tickColor);
                else
                    tex.SetPixel(x, y, trackDark);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    Sprite CreateThumbSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color fill = Color.white;
        Color border = new Color(0.6f, 0.6f, 0.6f);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                    tex.SetPixel(x, y, border);
                else
                    tex.SetPixel(x, y, fill);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(trackLength, 0.3f, 0f));
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.75f, Vector3.one * thumbSize);
    }
}
