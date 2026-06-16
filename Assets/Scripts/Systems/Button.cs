using UnityEngine;

public class Button : MonoBehaviour
{
    public enum ButtonMode
    {
        Toggle,   // Activates for a duration, then auto-deactivates
        Hold      // Active only while mouse is held down (or sticker is placed)
    }

    [Header("Targets")]
    public MonoBehaviour[] targets;

    [Header("Cursor Reference")]
    public CursorController cursorController;

    [Header("Button Mode")]
    public ButtonMode mode = ButtonMode.Hold;

    [Header("Toggle Settings")]
    public float toggleDuration = 3f;

    // Internal state
    private bool isOn = false;
    private bool isBeingHeld = false;   // True while mouse is held over this button
    private float toggleTimer = 0f;

    void Update()
    {
        // Timer countdown for Toggle mode
        if (mode == ButtonMode.Toggle && isOn)
        {
            toggleTimer -= Time.deltaTime;
            if (toggleTimer <= 0f)
            {
                isOn = false;
                SetTargets(false);
            }
        }

        if (cursorController == null || !cursorController.isCursorMode)
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        bool mouseOverThis = hit.collider != null && hit.collider.gameObject == gameObject;

        if (mode == ButtonMode.Toggle)
        {
            if (Input.GetMouseButtonDown(0) && mouseOverThis)
                HandleToggleClick();
        }
        else if (mode == ButtonMode.Hold)
        {
            if (Input.GetMouseButtonDown(0) && mouseOverThis)
                StartHold();

            if (Input.GetMouseButtonUp(0) && isBeingHeld)
                EndHold();
        }
    }

    // --- Toggle logic (timed) ---

    void HandleToggleClick()
    {
        toggleTimer = toggleDuration;

        if (!isOn)
        {
            isOn = true;
            SetTargets(true);
        }
    }

    // --- Hold logic (momentary) ---

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

    public void HoldActivate()
    {
        if (mode == ButtonMode.Toggle)
        {
            toggleTimer = toggleDuration;
            if (!isOn)
            {
                isOn = true;
                SetTargets(true);
            }
        }
        else
        {
            SetTargets(true);
        }
    }

    public void HoldRelease()
    {
        if (mode == ButtonMode.Hold && !isBeingHeld)
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
