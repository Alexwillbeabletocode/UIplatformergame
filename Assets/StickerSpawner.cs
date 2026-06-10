using UnityEngine;

public class StickerSpawner : MonoBehaviour
{
    [Header("References")]
    public CursorController cursorController;

    [Header("Sticker Settings")]
    public GameObject stickerPrefab;
    public int maxStickers = 3;
    public float stickerLifetime = 3f;
    public float placementCooldown = 1.5f;

    private int activeStickers = 0;
    private float cooldownTimer = 0f;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (cursorController == null || !cursorController.isCursorMode)
            return;

        if (Input.GetMouseButtonDown(1) && cooldownTimer <= 0f)
            TrySpawnSticker();
    }

    void TrySpawnSticker()
    {
        if (activeStickers >= maxStickers)
            return;

        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        spawnPos.z = 0f;

        GameObject sticker = Instantiate(stickerPrefab, spawnPos, Quaternion.identity);
        Sticker stickerScript = sticker.GetComponent<Sticker>();
        if (stickerScript != null)
        {
            stickerScript.lifetime = stickerLifetime;
            stickerScript.spawner = this;
        }

        activeStickers++;
        cooldownTimer = placementCooldown;
    }

    public void OnStickerDestroyed()
    {
        activeStickers = Mathf.Max(0, activeStickers - 1);
    }

    public void ForceCooldown()
    {
        cooldownTimer = placementCooldown;
    }
}
