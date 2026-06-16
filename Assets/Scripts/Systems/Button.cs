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

    [Header("Sticker Slot")]
    public GameObject stickerSlotPrefab;
    public float slotDimAlpha = 0.3f;

    // Internal state
    private bool isOn = false;
    private bool isBeingHeld = false;
    private bool hasSticker = false;
    private float toggleTimer = 0f;
    private SpriteRenderer slotSr;

    void Start()
    {
        if (stickerSlotPrefab != null)
        {
            GameObject slot = Instantiate(stickerSlotPrefab, transform);
            slot.transform.localPosition = Vector3.zero;
            slotSr = slot.GetComponent<SpriteRenderer>();
        }

        UpdateSlot();
    }

    void Update()
    {
        UpdateSlot();

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
        hasSticker = true;
        UpdateSlot();

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
        hasSticker = false;
        UpdateSlot();

        if (mode == ButtonMode.Hold && !isBeingHeld)
            SetTargets(false);
    }

    // --- Slot visibility ---

    void UpdateSlot()
    {
        if (slotSr == null) return;

        float alpha;
        if (hasSticker)
            alpha = 0.05f;
        else if (cursorController != null && cursorController.isCursorMode)
            alpha = 1f;
        else
            alpha = slotDimAlpha;

        Color c = slotSr.color;
        c.a = alpha;
        slotSr.color = c;
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
