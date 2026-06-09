using UnityEngine;

public class Button : MonoBehaviour
{
    public enum ButtonMode
    {
        Toggle,   // Click to flip on/off
        Hold      // Active only while mouse is held down (or sticker is placed)
    }

    [Header("Targets")]
    public MonoBehaviour[] targets;

    [Header("Cursor Reference")]
    public CursorController cursorController;

    [Header("Button Mode")]
    public ButtonMode mode = ButtonMode.Hold;

    // Internal state
    private bool isOn = false;
    private bool isBeingHeld = false;   // True while mouse is held over this button

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        bool mouseOverThis = hit.collider != null && hit.collider.gameObject == gameObject;

        if (mode == ButtonMode.Toggle)
        {
            if (Input.GetMouseButtonDown(0) && mouseOverThis)
                HandleToggle();
        }
        else if (mode == ButtonMode.Hold)
        {
            if (Input.GetMouseButtonDown(0) && mouseOverThis)
                StartHold();

            // Release if mouse button is lifted, regardless of position
            if (Input.GetMouseButtonUp(0) && isBeingHeld)
                EndHold();
        }
    }

    // --- Toggle logic ---

    void HandleToggle()
    {
        isOn = !isOn;
        SetTargets(isOn);
    }

    // --- Hold logic ---

    void StartHold()
    {
        if (isBeingHeld) return;
        isBeingHeld = true;
        SetTargets(true);
    }

    void EndHold()
    {
        isBeingHeld = false;
        SetTargets(false);
    }

    // --- Sticker interface ---
    // Called by Sticker.cs when it lands on this button

    public void HoldActivate()
    {
        SetTargets(true);
    }

    public void HoldRelease()
    {
        // Only release if the player isn't also physically holding the button
        if (!isBeingHeld)
            SetTargets(false);
    }

    // --- Shared helper ---

    void SetTargets(bool activate)
    {
        foreach (MonoBehaviour target in targets)
        {
            IInteractable interactable = target as IInteractable;
            if (interactable == null) continue;

            if (activate)
                interactable.Activate();
            else
                interactable.Deactivate();
        }
    }
}