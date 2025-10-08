using UnityEngine;
using TMPro;

public class ClickPopupSpawner : MonoBehaviour
{
    public RectTransform canvasRectTransform;
    public void SpawnPopup(Vector2 worldPosition, string text)
    {
        // World -> Screen
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        // Screen -> Local (Camera mode obligāti jāpadod kamera)
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPos,
            canvasRectTransform.GetComponentInParent<Canvas>().worldCamera,
            out localPos);

        // Izveido popup
        GameObject popupObj = new GameObject("ClickPopup",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        popupObj.transform.SetParent(canvasRectTransform, false);

        // Pozīcija
        RectTransform rectTransform = popupObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = localPos;
        rectTransform.sizeDelta = new Vector2(100, 40);

        // Teksts
        TextMeshProUGUI tmpText = popupObj.GetComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(1f, 0.8f, 0.2f, 1f);

        // Animācija
        popupObj.AddComponent<ClickPopupAnimation>();
    }
}