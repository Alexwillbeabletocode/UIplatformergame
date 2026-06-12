using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public Text livesText;
    public PlayerMovement player;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (player == null || livesText == null) return;

        string hearts = "";
        for (int i = 0; i < player.lives; i++)
            hearts += "\u2665 ";
        if (player.lives <= 0) hearts = "\u2661";
        livesText.text = hearts;
    }
}
