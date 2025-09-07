using UnityEngine;
using TMPro;

public class ClickPopupSpawner : MonoBehaviour
{
    public RectTransform canvasRectTransform;

    public void SpawnPopup(Vector2 worldPosition, string text)
    {
        // Convert world position to screen position
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        // Convert screen position to Canvas local position
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPos, null, out localPos);

        // Create new GameObject for popup
        GameObject popupObj = new GameObject("ClickPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        popupObj.transform.SetParent(canvasRectTransform, false);

        // Set position
        RectTransform rectTransform = popupObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = localPos;
        rectTransform.sizeDelta = new Vector2(100, 40);

        // Configure TextMeshProUGUI
        TextMeshProUGUI tmpText = popupObj.GetComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(1f, 0.8f, 0.2f, 1f); // Gold-like color

        // Add animation script
        popupObj.AddComponent<ClickPopupAnimation>();
    }
}