using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;
    public CursorController cursorController;

    [Header("Follow Settings")]
    public float smoothTime = 0.2f;
    public Vector2 playerOffset = Vector2.zero;

    [Header("Cursor Mode")]
    [Range(0f, 0.8f)]
    public float cursorDeadZone = 0.3f;
    public float cursorFollowSpeed = 4f;
    public float playerMargin = 1f;
    public float hoverStopMultiplier = 0.1f;
    public LayerMask interactableLayers;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity = Vector3.zero;
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
            Vector3 playerCenter = playerTarget.position + (Vector3)playerOffset;
            Vector3 cursorPos = cursorController.transform.position;
            Vector3 offset = cursorPos - playerCenter;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector2 normalized = new Vector2(
                Mathf.Abs(offset.x) / halfW,
                Mathf.Abs(offset.y) / halfH
            );

            float maxNorm = Mathf.Max(normalized.x, normalized.y);
            Vector3 targetOffset = Vector3.zero;

            if (maxNorm > cursorDeadZone)
            {
                float excess = maxNorm - cursorDeadZone;
                float scale = excess / (1f - cursorDeadZone);
                targetOffset = offset * scale;
            }

            float clampH = halfH - playerMargin;
            float clampW = halfW - playerMargin;
            targetOffset.x = Mathf.Clamp(targetOffset.x, -clampW, clampW);
            targetOffset.y = Mathf.Clamp(targetOffset.y, -clampH, clampH);

            desiredPos = new Vector3(
                playerCenter.x + targetOffset.x,
                playerCenter.y + targetOffset.y,
                transform.position.z
            );

            // Slow camera when hovering an interactable or near a keyhole
            float speed = cursorFollowSpeed;
            if (interactableLayers.value != 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(cursorPos, Vector2.zero, 0f, interactableLayers);
                if (hit.collider != null)
                    speed *= hoverStopMultiplier;
            }
            speed *= Keyhole.GetSlowdownFactor(cursorPos);

            transform.position = Vector3.Lerp(
                transform.position, desiredPos, speed * Time.deltaTime
            );
        }
        else
        {
            desiredPos = new Vector3(
                playerTarget.position.x + playerOffset.x,
                playerTarget.position.y + playerOffset.y,
                transform.position.z
            );

            if (useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
                desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
            }

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref velocity, smoothTime
            );
        }
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
    }
}
