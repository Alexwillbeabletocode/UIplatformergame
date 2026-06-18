using UnityEngine;

public class CursorController : MonoBehaviour
{
    public enum CursorState
    {
        Arrow,
        Hand,
        HandDrag
    }

    [Header("Cursor Settings")]
    public bool isCursorMode = false;
    public SpriteRenderer cursorRenderer;

    [Header("Cursor Sprites")]
    public Sprite arrowSprite;
    public Sprite handSprite;
    public Sprite handDragSprite;

    [Header("Interactable Detection")]
    public LayerMask interactableLayers;

    [Header("Hide Delay")]
    public float hideDelay = 0.5f;
    private float hideTimer = 0f;

    [Header("Glitch")]
    public float glitchAmplitude = 0.5f;
    public float glitchFrequency = 20f;

    private float glitchTimer = 0f;
    private Vector3 basePosition;
    private bool isGlitching = false;
    private CursorState currentState = CursorState.Arrow;

    void Start()
    {
        if (cursorRenderer != null)
        {
            cursorRenderer.sprite = arrowSprite;
            cursorRenderer.sortingOrder = short.MaxValue;
        }
    }

    void Update()
    {
        if (isGlitching)
        {
            glitchTimer -= Time.deltaTime;
            float intensity = Mathf.Clamp01(glitchTimer * 2f);
            float shakeX = Mathf.PerlinNoise(Time.time * glitchFrequency, 0f) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0f, Time.time * glitchFrequency) * 2f - 1f;
            transform.position = basePosition + new Vector3(shakeX, shakeY, 0f) * glitchAmplitude * intensity;

            if (glitchTimer <= 0f)
            {
                isGlitching = false;
                transform.position = basePosition;
            }
        }
        else
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            transform.position = mouseWorld;
            basePosition = transform.position;
        }

        if (isCursorMode)
        {
            cursorRenderer.enabled = true;
            hideTimer = hideDelay;
            UpdateCursorState();
        }
        else
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                cursorRenderer.enabled = false;
        }
    }

    void UpdateCursorState()
    {
        bool mouseHeld = Input.GetMouseButton(0);

        if (mouseHeld)
        {
            if (currentState != CursorState.HandDrag)
                SetState(CursorState.HandDrag);
            return;
        }

        bool onInteractable = Physics2D.OverlapPoint(transform.position, interactableLayers) != null;
        if (!onInteractable)
            onInteractable = Keyhole.IsWorldPointHovered(transform.position);
        SetState(onInteractable ? CursorState.Hand : CursorState.Arrow);
    }

    void SetState(CursorState state)
    {
        currentState = state;
        if (cursorRenderer == null) return;

        Sprite sprite = null;
        switch (state)
        {
            case CursorState.Arrow: sprite = arrowSprite; break;
            case CursorState.Hand: sprite = handSprite; break;
            case CursorState.HandDrag: sprite = handDragSprite; break;
        }

        if (sprite != null)
            cursorRenderer.sprite = sprite;
    }

    public void SetCursorMode(bool value)
    {
        isCursorMode = value;
        cursorRenderer.enabled = isCursorMode;
        if (isCursorMode)
            SetState(CursorState.Arrow);
    }

    public void Glitch(float duration)
    {
        basePosition = transform.position;
        glitchTimer = duration;
        isGlitching = true;
    }
}
