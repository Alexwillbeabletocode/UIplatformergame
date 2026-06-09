using UnityEngine;

public class ModeSwitcher : MonoBehaviour
{
    [Header("References")]
    public CursorController cursorController; // Drag your Cursor object here

    void Update()
    {
        // Toggle cursor mode
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            cursorController.SetCursorMode(!cursorController.isCursorMode);
        }
    }
}