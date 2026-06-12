using UnityEngine;

public class ModeSwitcher : MonoBehaviour
{
    [Header("References")]
    public CursorController cursorController;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            cursorController.SetCursorMode(!cursorController.isCursorMode);

            if (cursorController.isCursorMode)
            {
                PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
                if (player != null)
                {
                    Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}