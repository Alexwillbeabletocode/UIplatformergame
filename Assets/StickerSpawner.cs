using UnityEngine;

// Attach to any persistent GameObject (e.g. the same one as ModeSwitcher).
// Drag your Sticker prefab and CursorController into the inspector.

public class StickerSpawner : MonoBehaviour
{
    [Header("References")]
    public CursorController cursorController;

    [Header("Sticker Settings")]
    public GameObject stickerPrefab;        // Prefab with Sticker.cs + SpriteRenderer
    public int maxStickers = 2;             // Max stickers alive at once
    public float stickerLifetime = 3f;      // Passed to each spawned Sticker

    private int activeStickers = 0;

    void Update()
    {
        if (cursorController == null || !cursorController.isCursorMode)
            return;

        if (Input.GetMouseButtonDown(1)) // Right-click to place
        {
            TrySpawnSticker();
        }
    }

    void TrySpawnSticker()
    {
        if (activeStickers >= maxStickers)
        {
            Debug.Log("Sticker limit reached!");
            return;
        }

        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        spawnPos.z = 0f;

        GameObject sticker = Instantiate(stickerPrefab, spawnPos, Quaternion.identity);

        // Set lifetime from spawner settings
        Sticker stickerScript = sticker.GetComponent<Sticker>();
        if (stickerScript != null)
            stickerScript.lifetime = stickerLifetime;

        activeStickers++;

        // Decrement count when the sticker destroys itself
        Destroy(sticker, stickerLifetime + 0.1f); // Small buffer for cleanup
        StartCoroutine(DecrementOnExpiry(stickerLifetime));
    }

    System.Collections.IEnumerator DecrementOnExpiry(float delay)
    {
        yield return new WaitForSeconds(delay);
        activeStickers = Mathf.Max(0, activeStickers - 1);
    }
}