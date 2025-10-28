using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // For new Input System
using TMPro;

public class ClickPopupSpawner : MonoBehaviour
{
    [Header("References")]
    public RectTransform canvasRectTransform;
    public Button targetButton; // assign in Inspector

    [Header("Popup Settings")]
    public string popupText = "+1";
    public Color popupColor = new Color(0f, 0f, 1f, 1f); // Blue text

    private void Start()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(OnButtonClicked);
        else
            Debug.LogWarning("⚠️ ClickPopupSpawner: No button assigned!");
    }

    private void OnButtonClicked()
    {
        SpawnPopupAtCursor(popupText);
    }

    private void SpawnPopupAtCursor(string text)
    {
        // ✅ Works for both old and new Input Systems
        Vector2 screenPos;

#if ENABLE_INPUT_SYSTEM
        screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        screenPos = Input.mousePosition;
#endif

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPos,
            canvasRectTransform.GetComponentInParent<Canvas>().worldCamera,
            out localPos);

        // Create popup object
        GameObject popupObj = new GameObject("ClickPopup",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        popupObj.transform.SetParent(canvasRectTransform, false);

        // Position
        RectTransform rectTransform = popupObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = localPos;
        rectTransform.sizeDelta = new Vector2(150, 50);

        // Text setup
        TextMeshProUGUI tmpText = popupObj.GetComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = popupColor;

        // ✅ Prevent blocking clicks
        tmpText.raycastTarget = false;

        // Optional animation
        popupObj.AddComponent<ClickPopupAnimation>();
    }

    // Backward compatibility
    public void SpawnPopup(Vector2 worldPosition, string text)
    {
        SpawnPopupAtCursor(text);
    }
}
