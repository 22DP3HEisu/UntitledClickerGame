using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ClickPopupSpawner : MonoBehaviour
{
    [Header("References")]
    public RectTransform canvasRectTransform;
    public Button targetButton;

    [Header("Popup Settings")]
    public Sprite popupImage;
    public Vector2 popupImageSize = new Vector2(64, 64); 

    private void Start()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(OnButtonClicked);
        else
            Debug.LogWarning("⚠️ ClickPopupSpawner: No button assigned!");
    }

    private void OnButtonClicked()
    {
        SpawnPopupAtCursor();
    }

    private Vector2 GetScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        // prefer touch if any touch exists, otherwise mouse/pointer
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            if (touches.Count > 0)
            {
                // use the first touch position
                return touches[0].position.ReadValue();
            }
        }

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        // Fallback
        return Vector2.zero;
#else
        // Old input system: touch takes priority
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;
        return Input.mousePosition;
#endif
    }

    private void SpawnPopupAtCursor()
    {
        if (canvasRectTransform == null)
        {
            Debug.LogError("ClickPopupSpawner: canvasRectTransform is not assigned.");
            return;
        }

        if (popupImage == null)
        {
            Debug.LogWarning("ClickPopupSpawner: popupImage is not assigned. Popup will be invisible.");
        }

        Vector2 screenPos = GetScreenPosition();

        // Get canvas and appropriate camera (null for Overlay)
        Canvas canvas = canvasRectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ClickPopupSpawner: No parent Canvas found for canvasRectTransform.");
            return;
        }
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            ? canvas.worldCamera
            : null;

        // Create popup object
        GameObject popupObj = new GameObject("ClickPopup",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        // Parent first to keep canvas scaling / layout consistent
        popupObj.transform.SetParent(canvasRectTransform, false);

        RectTransform rectTransform = popupObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = popupImageSize;

        // Convert screen point to world point in rectangle and place the popup there.
        Vector3 worldPos;
        bool gotWorld = RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, screenPos, cam, out worldPos);
        if (gotWorld)
        {
            popupObj.transform.position = worldPos;
        }
        else
        {
            // Fallback: place using local point conversion to anchoredPosition
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPos, cam, out localPos);
            rectTransform.anchoredPosition = localPos;
        }

        // Image setup
        Image img = popupObj.GetComponent<Image>();
        img.sprite = popupImage;
        img.raycastTarget = false;

        // Optional animation
        popupObj.AddComponent<ClickPopupAnimation>();
    }

    // Backward compatibility (spawns at cursor, ignores text)
    public void SpawnPopup(Vector2 worldPosition, string text)
    {
        SpawnPopupAtCursor();
    }
}