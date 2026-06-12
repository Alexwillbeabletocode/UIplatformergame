using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public PlayerMovement player;
    private Text livesText;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        // Create canvas if none exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("HUDCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create lives text
        GameObject textGO = new GameObject("LivesText");
        textGO.transform.SetParent(canvas.transform, false);
        livesText = textGO.AddComponent<Text>();
        livesText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        livesText.fontSize = 32;
        livesText.color = Color.white;
        livesText.alignment = TextAnchor.UpperLeft;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(200, 50);
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
