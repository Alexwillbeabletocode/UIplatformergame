using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("Cursor Settings")]
    public bool isCursorMode = false;
    public SpriteRenderer cursorRenderer;

    [Header("Hide Delay")]
    public float hideDelay = 0.5f;
    private float hideTimer = 0f;

    [Header("Glitch")]
    public float glitchAmplitude = 0.5f;
    public float glitchFrequency = 20f;

    private float glitchTimer = 0f;
    private Vector3 basePosition;
    private bool isGlitching = false;

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
        }
        else
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                cursorRenderer.enabled = false;
        }
    }

    public void SetCursorMode(bool value)
    {
        isCursorMode = value;
        cursorRenderer.enabled = isCursorMode;
    }

    public void Glitch(float duration)
    {
        basePosition = transform.position;
        glitchTimer = duration;
        isGlitching = true;
    }
}
