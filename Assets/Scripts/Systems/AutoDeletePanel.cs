using UnityEngine;

public class AutoDeletePanel : MonoBehaviour
{
    public float openDuration = 2f;
    public float slamDuration = 0.3f;
    public float restDuration = 1.5f;
    public Vector2 panelSize = new Vector2(4f, 3f);
    public float startYOffset = 6f;

    public Color warningColor = Color.red;
    public Color dangerColor = new Color(0.8f, 0f, 0f);

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private PlayerMovement player;
    private Collider2D playerCol;
    private enum State { Opening, Open, Slamming, Rest }
    private State state = State.Opening;
    private float timer;
    private bool killedThisCycle;

    void Start()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePanelSprite();
        sr.sortingOrder = 10;

        col = gameObject.AddComponent<BoxCollider2D>();
        col.size = panelSize;
        col.isTrigger = true;

        transform.localScale = new Vector3(panelSize.x, 0.01f, 1f);
        transform.position += Vector3.up * startYOffset;

        player = FindFirstObjectByType<PlayerMovement>();
        playerCol = player?.GetComponent<Collider2D>();

        timer = 0f;
        killedThisCycle = false;
    }

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case State.Opening:
                float openProgress = Mathf.Clamp01(timer / 0.3f);
                transform.localScale = new Vector3(panelSize.x, Mathf.Lerp(0.01f, panelSize.y, openProgress), 1f);

                if (timer >= 0.3f)
                {
                    state = State.Open;
                    timer = 0f;
                    sr.color = warningColor;
                }
                break;

            case State.Open:
                sr.color = Color.Lerp(warningColor, dangerColor, timer / openDuration);

                if (timer >= openDuration)
                {
                    state = State.Slamming;
                    timer = 0f;
                    killedThisCycle = false;
                }
                break;

            case State.Slamming:
                float slamProgress = Mathf.Clamp01(timer / slamDuration);
                transform.localScale = new Vector3(panelSize.x, Mathf.Lerp(panelSize.y, 0.01f, slamProgress), 1f);

                // Kill player inside during slam
                if (!killedThisCycle && playerCol != null && player != null)
                {
                    // Check roughly halfway through slam
                    if (slamProgress > 0.3f && slamProgress < 0.8f)
                    {
                        ColliderDistance2D dist = Physics2D.Distance(playerCol, col);
                        if (dist.distance < 0.01f && dist.isValid)
                        {
                            player.Die();
                            killedThisCycle = true;
                        }
                    }
                }

                if (timer >= slamDuration)
                {
                    state = State.Rest;
                    timer = 0f;
                    sr.color = dangerColor * 0.3f;
                }
                break;

            case State.Rest:
                if (timer >= restDuration)
                {
                    ResetPanel();
                }
                break;
        }
    }

    void ResetPanel()
    {
        state = State.Opening;
        timer = 0f;
        sr.color = Color.white;
        killedThisCycle = false;
        transform.localScale = new Vector3(panelSize.x, 0.01f, 1f);
    }

    Sprite CreatePanelSprite()
    {
        int w = 128, h = 96;
        Texture2D tex = new Texture2D(w, h);
        Color bg = new Color(0.15f, 0.15f, 0.15f);
        Color titleBar = new Color(0.1f, 0.4f, 0.8f);
        Color border = Color.white;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2)
                    tex.SetPixel(x, y, border);
                else if (y >= h - 20)
                    tex.SetPixel(x, y, titleBar);
                else
                    tex.SetPixel(x, y, bg);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, panelSize);
    }
}
