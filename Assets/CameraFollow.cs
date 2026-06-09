using UnityEngine;

// Attach to your Main Camera.
// Platformer mode: follows the player.
// Cursor mode: follows the cursor freely, but is clamped so the
// player never leaves the viewport. Bounces when it hits that limit.

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;
    public CursorController cursorController;

    [Header("Follow Settings")]
    public float smoothTime = 0.2f;
    public Vector2 playerOffset = Vector2.zero;

    [Header("Cursor Mode — Player Visibility")]
    public float playerMargin = 1f;     // How close to the edge the player is allowed to get (world units)

    [Header("Cursor Mode — Boundary Bounce")]
    public float bounceScale = 0.3f;        // Strength of the bounce visual
    public float bounceSettleTime = 0.1f;   // How fast the bounce springs back

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity = Vector3.zero;
    private Vector3 bounceOffset = Vector3.zero;
    private Vector3 bounceVelocity = Vector3.zero;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (playerTarget == null) return;

        bool inCursorMode = cursorController != null && cursorController.isCursorMode;

        Vector3 desiredPos;

        if (inCursorMode)
        {
            // Start by following the cursor directly
            desiredPos = new Vector3(
                cursorController.transform.position.x,
                cursorController.transform.position.y,
                transform.position.z
            );

            // --- Clamp camera so player stays in viewport ---
            float halfH = cam.orthographicSize - playerMargin;
            float halfW = halfH * cam.aspect - playerMargin;

            float minX = playerTarget.position.x - halfW;
            float maxX = playerTarget.position.x + halfW;
            float minY = playerTarget.position.y - halfH;
            float maxY = playerTarget.position.y + halfH;

            Vector3 clampedPos = new Vector3(
                Mathf.Clamp(desiredPos.x, minX, maxX),
                Mathf.Clamp(desiredPos.y, minY, maxY),
                transform.position.z
            );

            // How far the cursor is pushing the camera past its allowed range
            Vector3 excess = desiredPos - clampedPos;

            // Bounce: camera squishes inward when held at the limit, springs back when cursor retreats
            Vector3 bounceTarget = excess.magnitude > 0.05f
                ? -Vector3.ClampMagnitude(excess, Mathf.Min(halfW, halfH) * bounceScale)
                : Vector3.zero;

            bounceOffset = Vector3.SmoothDamp(
                bounceOffset, bounceTarget, ref bounceVelocity, bounceSettleTime
            );

            desiredPos = clampedPos + bounceOffset;
        }
        else
        {
            // Platformer mode — standard player follow, no bounce
            bounceOffset = Vector3.zero;
            bounceVelocity = Vector3.zero;

            desiredPos = new Vector3(
                playerTarget.position.x + playerOffset.x,
                playerTarget.position.y + playerOffset.y,
                transform.position.z
            );
        }

        desiredPos.z = transform.position.z;

        if (useBounds)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref velocity, smoothTime
        );
    }

    public void SnapToTarget()
    {
        if (playerTarget == null) return;
        transform.position = new Vector3(
            playerTarget.position.x + playerOffset.x,
            playerTarget.position.y + playerOffset.y,
            transform.position.z
        );
        velocity = Vector3.zero;
        bounceOffset = Vector3.zero;
        bounceVelocity = Vector3.zero;
    }
}