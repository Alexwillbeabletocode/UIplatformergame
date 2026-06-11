using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    private Checkpoint currentCheckpoint;
    private Vector3 defaultSpawn;

    public float deathDelay = 0.3f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            defaultSpawn = player.transform.position;
    }

    public void SetCheckpoint(Checkpoint cp)
    {
        currentCheckpoint = cp;
    }

    public void RespawnPlayer()
    {
        GameObject player = FindFirstObjectByType<PlayerMovement>()?.gameObject;
        if (player == null) return;

        Vector3 spawnPos = currentCheckpoint != null ? currentCheckpoint.respawnPosition : defaultSpawn;
        player.transform.position = spawnPos;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
