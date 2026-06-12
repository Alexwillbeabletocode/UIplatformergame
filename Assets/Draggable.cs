using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable : MonoBehaviour
{
    public CursorController cursorController;

    [Header("Grid Settings")]
    public float gridSize = 1f;

    [Header("Axis Lock")]
    public bool lockX = false;
    public bool lockY = false;

    [Header("Movement")]
    public float followSpeed = 15f;
    public float maxDragSpeed = 8f;

    [Header("Placement")]
    public LayerMask blockingLayer;
    public Color validColor = Color.white;
    public Color invalidColor = Color.red;

    private bool isDragging = false;
    private Vector3 targetPosition;

    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private Vector3 lastValidPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        targetPosition = transform.position;
        lastValidPosition = transform.position;
        sr.color = validColor;
    }

    void FixedUpdate()
    {
        if (!isDragging) return;
        if (cursorController == null || !cursorController.isCursorMode)
        {
            isDragging = false;
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 newPos = Vector3.Lerp(currentPos, targetPosition, followSpeed * Time.fixedDeltaTime);

        // Cap speed
        Vector3 delta = newPos - currentPos;
        if (delta.magnitude > maxDragSpeed * Time.fixedDeltaTime)
            newPos = currentPos + delta.normalized * maxDragSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPos);
    }

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
        {
            isDragging = false;
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            Collider2D hit = Physics2D.OverlapBox(
                targetPosition,
                col.bounds.size * 0.9f,
                0f,
                blockingLayer
            );

            bool isValid = (hit == null || hit == col);

            if (isValid)
            {
                rb.MovePosition(targetPosition);
                lastValidPosition = targetPosition;
            }
            else
            {
                rb.MovePosition(lastValidPosition);
            }

            targetPosition = transform.position;
            sr.color = validColor;
        }

        if (isDragging)
        {
            Vector3 desired = mouseWorld;

            if (lockX) desired.x = transform.position.x;
            if (lockY) desired.y = transform.position.y;

            desired.x = Mathf.Round(desired.x / gridSize) * gridSize;
            desired.y = Mathf.Round(desired.y / gridSize) * gridSize;

            Vector3 direction = (desired - transform.position);
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                direction.Normalize();

                float stepSize = gridSize * 0.5f;
                int steps = Mathf.CeilToInt(distance / stepSize);

                Vector3 currentPos = transform.position;
                bool blocked = false;

                for (int i = 0; i < steps; i++)
                {
                    Vector3 nextPos = currentPos + direction * stepSize;

                    Collider2D hit = Physics2D.OverlapBox(
                        nextPos,
                        col.bounds.size * 0.9f,
                        0f,
                        blockingLayer
                    );

                    if (hit != null && hit != col)
                    {
                        blocked = true;
                        break;
                    }

                    currentPos = nextPos;
                }

                if (!blocked)
                {
                    targetPosition = desired;
                    lastValidPosition = desired;
                    sr.color = validColor;
                }
                else
                {
                    sr.color = invalidColor;
                }
            }
        }
    }
}
