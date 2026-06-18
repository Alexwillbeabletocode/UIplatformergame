using UnityEngine;
using System.Collections.Generic;

public class Keyhole : MonoBehaviour
{
    [Header("Targets")]
    public MonoBehaviour[] targets;

    [Header("Cursor Reference")]
    public CursorController cursorController;

    [Header("Sticker Slot (child Transform marking the hollow center)")]
    public Transform stickerSlot;

    [Header("Magnetic Slowdown")]
    public float magneticRadius = 0.5f;

    private static List<Keyhole> instances = new List<Keyhole>();

    private SpriteRenderer slotSr;
    private Color slotOriginalColor;
    private bool occupied = false;

    void Start()
    {
        instances.Add(this);
        if (stickerSlot != null)
        {
            slotSr = stickerSlot.GetComponent<SpriteRenderer>();
            if (slotSr != null)
                slotOriginalColor = slotSr.color;
        }
        UpdateSlot();
    }

    void OnDestroy()
    {
        instances.Remove(this);
    }

    void Update()
    {
        UpdateSlot();
    }

    // --- Sticker interface ---

    public void HoldActivate()
    {
        occupied = true;
        SetTargets(true);
        UpdateSlot();
    }

    public void HoldRelease()
    {
        occupied = false;
        SetTargets(false);
        UpdateSlot();
    }

    // --- Slot visual ---

    void UpdateSlot()
    {
        if (slotSr == null) return;

        bool inCursorMode = cursorController != null && cursorController.isCursorMode;
        bool hovering = inCursorMode && !occupied && IsMouseHovering();

        Color c = hovering ? Color.green : slotOriginalColor;
        c.a = 1f;
        slotSr.color = c;
    }

    public bool IsMouseHovering()
    {
        if (Camera.main == null || stickerSlot == null) return false;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return Vector2.Distance(mousePos, stickerSlot.position) < magneticRadius;
    }

    public static Keyhole GetHoveredKeyhole()
    {
        Keyhole closest = null;
        float closestDist = float.MaxValue;
        foreach (var kh in instances)
        {
            if (kh == null || kh.occupied || !kh.IsMouseHovering()) continue;
            float d = Vector2.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), kh.stickerSlot.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = kh;
            }
        }
        return closest;
    }

    public static bool IsWorldPointHovered(Vector2 worldPos)
    {
        foreach (var kh in instances)
        {
            if (kh == null || kh.occupied || kh.stickerSlot == null) continue;
            if (Vector2.Distance(worldPos, kh.stickerSlot.position) < kh.magneticRadius)
                return true;
        }
        return false;
    }

    // --- Magnetic slowdown (called by CameraFollow) ---

    public static float GetSlowdownFactor(Vector2 worldPos)
    {
        float minFactor = 1f;
        foreach (var kh in instances)
        {
            if (kh == null || kh.occupied) continue;
            float dist = Vector2.Distance(worldPos, kh.transform.position);
            if (dist < kh.magneticRadius)
            {
                float t = Mathf.Clamp01(dist / kh.magneticRadius);
                float factor = Mathf.Lerp(0.2f, 1f, t);
                if (factor < minFactor) minFactor = factor;
            }
        }
        return minFactor;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(stickerSlot != null ? stickerSlot.position : transform.position, magneticRadius);
    }
}
