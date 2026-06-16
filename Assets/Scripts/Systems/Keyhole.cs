using UnityEngine;
using System.Collections.Generic;

public class Keyhole : MonoBehaviour
{
    [Header("Targets")]
    public MonoBehaviour[] targets;

    [Header("Cursor Reference")]
    public CursorController cursorController;

    [Header("Slot Visual")]
    public float dimAlpha = 0.3f;

    [Header("Magnetic Slowdown")]
    public float magneticRadius = 0.5f;

    private static List<Keyhole> instances = new List<Keyhole>();

    private SpriteRenderer slotSr;
    private bool occupied = false;

    void Start()
    {
        instances.Add(this);
        slotSr = GetComponent<SpriteRenderer>();
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

    // --- Slot alpha ---

    void UpdateSlot()
    {
        if (slotSr == null) return;

        float alpha;
        if (occupied)
            alpha = 0.05f;
        else if (cursorController != null && cursorController.isCursorMode)
            alpha = 1f;
        else
            alpha = dimAlpha;

        Color c = slotSr.color;
        c.a = alpha;
        slotSr.color = c;
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
        Gizmos.DrawWireSphere(transform.position, magneticRadius);
    }
}
