using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ToggleDoor : MonoBehaviour, IInteractable
{
    private BoxCollider2D col;
    private SpriteRenderer sr;

    [Header("Door Settings")]
    public Color openColor = new Color(1f, 1f, 1f, 0.3f);  // Semi-transparent when open
    private Color closedColor;                               // Original sprite color when closed

    private bool isOpen = false;

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        closedColor = sr.color;

        // Door starts closed and solid
        col.isTrigger = false;
        sr.color = closedColor;
    }

    // Button pressed / sticker placed > open the door
    public void Activate()
    {
        isOpen = true;
        col.isTrigger = true;   // Passable
        sr.color = openColor;
    }

    // Button released / sticker expired > close the door
    public void Deactivate()
    {
        isOpen = false;
        col.isTrigger = false;  // Solid
        sr.color = closedColor;
    }

    public void Toggle()
    {
        if (isOpen) Deactivate();
        else Activate();
    }
}