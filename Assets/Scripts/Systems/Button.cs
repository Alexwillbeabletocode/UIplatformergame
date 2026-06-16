using UnityEngine;

public class Button : MonoBehaviour
{
    [Header("Targets")]
    public MonoBehaviour[] targets;

    [Header("Cursor Reference")]
    public CursorController cursorController;

    private bool isHeld = false;

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        bool mouseOverThis = hit.collider != null && hit.collider.gameObject == gameObject;

        if (Input.GetMouseButtonDown(0) && mouseOverThis)
        {
            isHeld = true;
            SetTargets(true);
        }

        if (Input.GetMouseButtonUp(0) && isHeld)
        {
            isHeld = false;
            SetTargets(false);
        }
    }

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
