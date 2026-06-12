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
    [Tooltip("Child transform whose Y scale goes from 0 to 100% as platform lifts. Set its sprite pivot to bottom-center so it grows upward.")]
    public Transform progressBar;

    private Vector3 startPosition;
    private Vector3 barBaseScale;
    private Collider2D col;
    private Rigidbody2D rb;

    private enum LiftState { Idle, Lifting, Pausing, Returning }
    private LiftState state = LiftState.Idle;
    private float pauseTimer = 0f;

    void Start()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        startPosition = transform.position;

        if (progressBar != null)
            barBaseScale = progressBar.localScale;

        UpdateProgressBar();
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
        if (progressBar == null || maxLiftHeight <= 0f) return;

        float offset = transform.position.y - startPosition.y;
        float progress = Mathf.Clamp01(offset / maxLiftHeight);

        Vector3 scale = barBaseScale;
        scale.y = barBaseScale.y * progress;
        progressBar.localScale = scale;
    }
}
