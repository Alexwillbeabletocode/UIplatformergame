using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ScrollbarPlatform : MonoBehaviour
{
    public CursorController cursorController;

    [Header("Movement")]
    public float maxTravelDistance = 5f;

    [Header("References")]
    public Transform dragButton;
    public Transform trackContainer;

    private Vector3 startPosition;
    private BoxCollider2D boxCol;
    private Rigidbody2D rb;

    private bool isDragging = false;
    private float dragOffset;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Collider2D playerCol;

    private float platformHalfWidth;
    private float clampMinX;
    private float clampMaxX;

    void Start()
    {
        boxCol = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        startPosition = transform.position;
        platformHalfWidth = boxCol.bounds.extents.x;

        if (trackContainer != null)
        {
            float left = float.MaxValue, right = float.MinValue;
            Collider2D containerCol = trackContainer.GetComponent<Collider2D>();
            if (containerCol != null)
            {
                left = containerCol.bounds.min.x;
                right = containerCol.bounds.max.x;
            }
            else
            {
                SpriteRenderer containerSr = trackContainer.GetComponent<SpriteRenderer>();
                if (containerSr != null)
                {
                    left = containerSr.bounds.min.x;
                    right = containerSr.bounds.max.x;
                }
            }

            clampMinX = left + platformHalfWidth;
            clampMaxX = right - platformHalfWidth;
        }
        else
        {
            clampMinX = startPosition.x - maxTravelDistance + platformHalfWidth;
            clampMaxX = startPosition.x + maxTravelDistance - platformHalfWidth;
        }
    }

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        if (cursorController == null || !cursorController.isCursorMode)
        {
            isDragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null)
            {
                if (dragButton != null && hit.collider.transform == dragButton)
                {
                    isDragging = true;
                    dragOffset = transform.position.x - mouseWorld.x;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    void FixedUpdate()
    {
        Vector2 delta = Vector2.zero;

        if (rb.position.y != startPosition.y)
            rb.MovePosition(new Vector2(rb.position.x, startPosition.y));

        if (isDragging)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float targetX = mouseWorld.x + dragOffset;
            targetX = Mathf.Clamp(targetX, clampMinX, clampMaxX);
            float dx = targetX - rb.position.x;
            delta = new Vector2(dx, 0f);
            rb.MovePosition(new Vector2(targetX, startPosition.y));
            if (dragButton != null)
                dragButton.position += new Vector3(dx, 0f, 0f);
        }

        if (delta.magnitude > 0.0001f)
            CarryPlayer(delta);
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
