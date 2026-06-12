using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable : MonoBehaviour
{
    public CursorController cursorController;

    [Header("Lift Settings")]
    public float maxLiftHeight = 5f;
    public float liftSpeed = 3f;
    public float returnSpeed = 5f;
    public float pauseDuration = 1f;

    [Header("Progress Bar")]
    public Color barColor = new Color(0f, 0.7f, 1f, 0.8f);
    public float barInset = 0.1f;

    private Vector3 startPosition;
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;
    private SpriteRenderer barSr;

    private enum LiftState { Idle, Lifting, Pausing, Returning }
    private LiftState state = LiftState.Idle;
    private float pauseTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        startPosition = transform.position;
        CreateProgressBar();
    }

    void CreateProgressBar()
    {
        GameObject barGO = new GameObject("LiftProgress");
        barGO.transform.SetParent(transform);
        barSr = barGO.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        barSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0f));
        barSr.color = barColor;
        barSr.sortingOrder = sr.sortingOrder + 1;

        float h = col.bounds.size.y;
        float w = col.bounds.size.x;
        barGO.transform.localPosition = new Vector3(0, -h / 2f + barInset, 0);
        barGO.transform.localScale = new Vector3(w - barInset * 2f, 0f, 1f);
    }

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
        {
            if (state == LiftState.Lifting)
            {
                state = LiftState.Pausing;
                pauseTimer = pauseDuration;
            }
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
                state = LiftState.Lifting;
        }

        if (Input.GetMouseButtonUp(0) && state == LiftState.Lifting)
        {
            state = LiftState.Pausing;
            pauseTimer = pauseDuration;
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case LiftState.Lifting:
                float offset = transform.position.y - startPosition.y;
                float remaining = maxLiftHeight - offset;

                if (remaining <= 0f)
                {
                    state = LiftState.Pausing;
                    pauseTimer = pauseDuration;
                    break;
                }

                float step = Mathf.Min(liftSpeed * Time.fixedDeltaTime, remaining);
                rb.MovePosition(rb.position + Vector2.up * step);
                UpdateProgressBar();
                break;

            case LiftState.Pausing:
                pauseTimer -= Time.fixedDeltaTime;
                if (pauseTimer <= 0f)
                    state = LiftState.Returning;
                break;

            case LiftState.Returning:
                Vector2 newPos = Vector2.MoveTowards(rb.position, startPosition, returnSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newPos);
                UpdateProgressBar();
                if (Vector2.Distance(rb.position, startPosition) < 0.01f)
                {
                    rb.MovePosition(startPosition);
                    state = LiftState.Idle;
                    UpdateProgressBar();
                }
                break;
        }
    }

    void UpdateProgressBar()
    {
        if (barSr == null || maxLiftHeight <= 0f) return;

        float offset = transform.position.y - startPosition.y;
        float progress = Mathf.Clamp01(offset / maxLiftHeight);

        float h = col.bounds.size.y;
        float maxBarHeight = Mathf.Max(0f, h - barInset * 2f);
        float barHeight = Mathf.Lerp(0f, maxBarHeight, progress);

        Vector3 scale = barSr.transform.localScale;
        scale.y = barHeight;
        barSr.transform.localScale = scale;
    }
}
