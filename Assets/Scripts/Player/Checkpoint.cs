using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Vector3 respawnPosition => transform.position;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RespawnManager.Instance.SetCheckpoint(this);
        }
    }
}
