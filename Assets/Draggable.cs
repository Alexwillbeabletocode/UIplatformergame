using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable : MonoBehaviour
{
    public enum MoveAxis { X, Y }
    public enum MoveDirection { Positive, Negative }

    public CursorController cursorController;

    [Header("Movement")]
    public MoveAxis moveAxis = MoveAxis.Y;
    public MoveDirection moveDirection = MoveDirection.Positive;
    public float maxTravelDistance = 5f;
    public float moveSpeed = 3f;
    public float returnSpeed = 5f;
    public float pauseDuration = 1f;

    [Header("Progress Bar")]
    [Tooltip("Child transform whose Y scale goes from 0 to 100% as platform moves. Set its sprite pivot to bottom-center so it grows upward.")]
    public Transform progressBar;

    private Vector3 startPosition;
    private Vector3 barBaseScale;
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private enum LiftState { Idle, Lifting, Pausing, Returning }
    private LiftState state = LiftState.Idle;
    private float pauseTimer = 0f;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Collider2D playerCol;

    private Vector2 MoveVector
    {
        get
        {
            Vector2 dir = moveAxis == MoveAxis.X ? Vector2.right : Vector2.up;
            return moveDirection == MoveDirection.Positive ? dir : -dir;
        }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        startPosition = transform.position;

        if (progressBar != null)
        {
            barBaseScale = progressBar.localScale;
            SpriteRenderer barSr = progressBar.GetComponent<SpriteRenderer>();
            if (barSr != null)
                barSr.sortingOrder = sr.sortingOrder + 1;
        }

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
        Vector2 prevPos = rb.position;

        switch (state)
        {
            case LiftState.Lifting:
                float remaining = maxTravelDistance - TravelOffset;

                if (remaining <= 0f)
                {
                    state = LiftState.Pausing;
                    pauseTimer = pauseDuration;
                    break;
                }

                float step = Mathf.Min(moveSpeed * Time.fixedDeltaTime, remaining);
                rb.MovePosition(rb.position + MoveVector * step);
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

        CarryPlayer(rb.position - prevPos);
    }

    private float TravelOffset
    {
        get
        {
            float raw = moveAxis == MoveAxis.X
                ? transform.position.x - startPosition.x
                : transform.position.y - startPosition.y;
            return moveDirection == MoveDirection.Positive ? raw : -raw;
        }
    }

    void UpdateProgressBar()
    {
        if (progressBar == null || maxTravelDistance <= 0f) return;

        float progress = Mathf.Clamp01(TravelOffset / maxTravelDistance);

        Vector3 scale = barBaseScale;
        scale.y = barBaseScale.y * progress;
        progressBar.localScale = scale;
    }

    void CarryPlayer(Vector2 delta)
    {
        if (delta.magnitude < 0.0001f) return;

        if (playerTransform == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;
            playerTransform = pm.transform;
            playerRb = pm.GetComponent<Rigidbody2D>();
            playerCol = pm.GetComponent<Collider2D>();
        }

        if (playerCol == null || col == null) return;

        if (playerCol.bounds.Intersects(col.bounds))
            playerRb.position += delta;
    }
}
