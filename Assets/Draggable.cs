using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable : MonoBehaviour
{
    public enum MoveDirection { Positive, Negative }

    public CursorController cursorController;

    [Header("Movement")]
    public MoveDirection moveDirection = MoveDirection.Positive;
    public float maxTravelDistance = 5f;
    public float moveSpeed = 3f;
    public float returnSpeed = 5f;
    public float pauseDuration = 1f;

    [Header("Progress Bar")]
    [Tooltip("Child transform whose Y scale goes from 0 to 100% as platform moves. Set sprite pivot to bottom-center so it grows upward.")]
    public Transform progressBar;

    [Header("Stretch Visual")]
    public bool enableStretchVisual = false;

    private Vector3 startPosition;
    private Vector3 barBaseScale;
    private SpriteRenderer sr;
    private BoxCollider2D boxCol;
    private Rigidbody2D rb;

    private enum LiftState { Idle, Lifting, Pausing, Returning }
    private LiftState state = LiftState.Idle;
    private float pauseTimer = 0f;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Collider2D playerCol;

    private float naturalWidth;
    private float naturalHeight;
    private Vector2 originalColOffset;
    private Vector2 originalColSize;
    private float currentTravel = 0f;

    private Vector2 MoveVector
    {
        get { return moveDirection == MoveDirection.Positive ? Vector2.up : Vector2.down; }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();
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

        if (enableStretchVisual && sr.sprite != null)
        {
            naturalWidth = sr.bounds.size.x;
            naturalHeight = sr.bounds.size.y;
            originalColOffset = boxCol.offset;
            originalColSize = boxCol.size;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(naturalWidth, naturalHeight);
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
        Vector2 delta = Vector2.zero;

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

                if (enableStretchVisual)
                {
                    currentTravel += step;
                    delta = MoveVector * step;
                    UpdateProgressBar();
                    UpdateStretchVisual();
                }
                else
                {
                    delta = MoveVector * step;
                    rb.MovePosition(rb.position + delta);
                    UpdateProgressBar();
                }
                break;

            case LiftState.Pausing:
                pauseTimer -= Time.fixedDeltaTime;
                if (pauseTimer <= 0f)
                    state = LiftState.Returning;
                break;

            case LiftState.Returning:
                if (enableStretchVisual)
                {
                    float stepBack = returnSpeed * Time.fixedDeltaTime;
                    float prev = currentTravel;
                    currentTravel = Mathf.Max(0f, currentTravel - stepBack);
                    delta = MoveVector * (currentTravel - prev);
                    UpdateProgressBar();
                    UpdateStretchVisual();
                    if (currentTravel <= 0.01f)
                    {
                        state = LiftState.Idle;
                        UpdateProgressBar();
                        UpdateStretchVisual();
                    }
                }
                else
                {
                    Vector2 targetPos = Vector2.MoveTowards(rb.position, startPosition, returnSpeed * Time.fixedDeltaTime);
                    delta = targetPos - rb.position;
                    rb.MovePosition(targetPos);
                    UpdateProgressBar();
                    if (Vector2.Distance(targetPos, startPosition) < 0.01f)
                    {
                        state = LiftState.Idle;
                        UpdateProgressBar();
                    }
                }
                break;
        }

        if (enableStretchVisual && delta.magnitude > 0.0001f)
            CarryPlayer(delta);
    }

    private float TravelOffset
    {
        get
        {
            if (enableStretchVisual)
                return currentTravel;

            float raw = transform.position.y - startPosition.y;
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

    void UpdateStretchVisual()
    {
        if (!enableStretchVisual) return;

        float travel = currentTravel;
        if (travel > 0.01f)
        {
            sr.size = new Vector2(naturalWidth, naturalHeight + travel);
            boxCol.offset = new Vector2(originalColOffset.x, originalColOffset.y + travel / 2f);
            boxCol.size = new Vector2(originalColSize.x, originalColSize.y + travel);
        }
        else
        {
            sr.size = new Vector2(naturalWidth, naturalHeight);
            boxCol.offset = originalColOffset;
            boxCol.size = originalColSize;
        }
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

        if (playerCol == null || boxCol == null) return;

        bool touching = playerCol.IsTouching(boxCol);

        if (!touching)
        {
            Bounds pb = playerCol.bounds;
            Bounds plb = boxCol.bounds;
            bool vertical = pb.min.y <= plb.max.y + 0.1f && pb.min.y >= plb.min.y;
            bool horizontal = pb.max.x > plb.min.x && pb.min.x < plb.max.x;
            touching = vertical && horizontal;
        }

        if (touching)
            playerRb.position += delta;
    }
}
