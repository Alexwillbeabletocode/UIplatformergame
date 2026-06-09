using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("Cursor Settings")]
    public bool isCursorMode = false;
    public SpriteRenderer cursorRenderer;

    [Header("Hide Delay")]
    public float hideDelay = 0.5f;
    private float hideTimer = 0f;

    void Update()
    {
        // Cursor always freely follows the mouse — no clamping here
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        transform.position = mouseWorld;

        if (isCursorMode)
        {
            cursorRenderer.enabled = true;
            hideTimer = hideDelay;
        }
        else
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                cursorRenderer.enabled = false;
        }
    }

    public void SetCursorMode(bool value)
    {
        isCursorMode = value;
        cursorRenderer.enabled = isCursorMode;
    }
}