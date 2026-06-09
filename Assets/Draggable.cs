using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
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

    [Header("Placement")]
    public LayerMask blockingLayer;
    public Color validColor = Color.white;
    public Color invalidColor = Color.red;

    private bool isDragging = false;
    private Vector3 targetPosition;

    private SpriteRenderer sr;
    private Collider2D col;

    private Vector3 lastValidPosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        targetPosition = transform.position;

        lastValidPosition = transform.position; 
    }

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // Start dragging
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                isDragging = true;
            }
        }

        // Stop dragging
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            // Check if final position is valid
            Collider2D hit = Physics2D.OverlapBox(
                targetPosition,
                col.bounds.size * 0.9f,
                0f,
                blockingLayer
            );

            bool isValid = (hit == null || hit == col);

            // Snap to correct position
            if (isValid)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = lastValidPosition;
            }

            // Reset color
            sr.color = validColor;
        }

        if (isDragging)
        {
            Vector3 desired = mouseWorld;

            // Axis locking
            if (lockX) desired.x = transform.position.x;
            if (lockY) desired.y = transform.position.y;

            // Grid snapping
            desired.x = Mathf.Round(desired.x / gridSize) * gridSize;
            desired.y = Mathf.Round(desired.y / gridSize) * gridSize;

            Vector3 direction = (desired - transform.position);
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                direction.Normalize();

                float stepSize = gridSize * 0.5f; // smaller = safer
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

                // Smooth movement
                transform.position = Vector3.Lerp(
                    transform.position,
                    currentPos,
                    followSpeed * Time.deltaTime
                );
            }
        }
        else
            {
                transform.position = Vector3.Lerp(
                transform.position,
                lastValidPosition,
                followSpeed * Time.deltaTime
);
                // DON'T update position if invalid
                // stays at last valid spot
            }
        }
    }
