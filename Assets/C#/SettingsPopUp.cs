using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SettingsPopUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Button Achievments;
    [SerializeField] private Transform SettingsPanel;

    [SerializeField] private int pageNumber;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    // msg controls whether clicking opens the settings panel (1 = enabled)
    [SerializeField] private int msg = 0;

    private Vector3 startScale;
    private Image image;

    private void Awake()
    {
        startScale = transform.localScale;
        image = GetComponent<Image>();

        Debug.Log($"[SettingsPopUp] Awake - msg={msg}, startScale={startScale}, image={(image != null ? "found" : "null")}");

        // If there's no Image (so Unity UI can't receive pointer events), add a transparent Image so UI raycasts work.
        if (image == null)
        {
            // Ensure a CanvasRenderer exists (required by UI Graphic components)
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();

            image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f); // fully transparent, visible to raycasts
            image.raycastTarget = true;

            Debug.Log("[SettingsPopUp] Awake - Image was missing. Added transparent Image so clicks register.");
        }

        if (SettingsPanel == null)
        {
            Debug.LogWarning("[SettingsPopUp] Awake - SettingsPanel reference is NULL. Assign it in the inspector.");
        }
        else
        {
            bool wasActive = SettingsPanel.gameObject.activeSelf;
            SettingsPanel.gameObject.SetActive(false);
            Debug.Log($"[SettingsPopUp] Awake - SettingsPanel wasActive={wasActive} -> forcibly set to false");
        }

        // Check for EventSystem and Canvas GraphicRaycaster presence (common causes of non-clickable UI)
        if (FindObjectOfType<EventSystem>() == null)
            Debug.LogWarning("[SettingsPopUp] Awake - No EventSystem found in scene. Add one via GameObject -> UI -> Event System.");

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogWarning("[SettingsPopUp] Awake - No parent Canvas found. UI must be inside a Canvas to receive pointer events.");
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
            Debug.LogWarning("[SettingsPopUp] Awake - Parent Canvas has no GraphicRaycaster. Add one to enable UI raycasts.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SettingsPopUp] OnPointerClick - eventData={(eventData != null ? "present" : "null")}, msg={msg}");

        float duration = 0.1f;

        LeanTween.scale(gameObject, startScale * 0.9f, duration)
        .setEaseOutQuad()
        .setOnComplete(() =>
        {
            Debug.Log("[SettingsPopUp] click shrink complete - starting restore scale");
            LeanTween.scale(gameObject, startScale, duration).setEaseInQuad();

            // Toggle settings panel only when msg == 1
            if (msg == 1)
            {
                ToggleSettingsPanel();
            }
            else
            {
                Debug.Log($"[SettingsPopUp] OnPointerClick - msg != 1 (msg={msg}), will not toggle SettingsPanel.");
            }
        });
    }

    private void ToggleSettingsPanel()
    {
        if (SettingsPanel == null)
        {
            Debug.LogWarning("[SettingsPopUp] ToggleSettingsPanel - SettingsPanel reference is null.");
            return;
        }

        bool isActive = SettingsPanel.gameObject.activeSelf;
        if (isActive)
        {
            SettingsPanel.gameObject.SetActive(false);
            Debug.Log("[SettingsPopUp] ToggleSettingsPanel - SettingsPanel closed.");
        }
        else
        {
            // Activate panel
            SettingsPanel.gameObject.SetActive(true);

            // Ensure this popup stays on top so it remains clickable even when the panel is shown.
            // This helps if the panel covers the popup area and blocks further clicks.
            transform.SetAsLastSibling();

            Debug.Log("[SettingsPopUp] ToggleSettingsPanel - SettingsPanel opened. Popup brought to front to remain clickable.");
        }
    }

    private void OnPageChanged(int currentPage)
    {
        SetActiveState(currentPage == pageNumber);
    }

    private void SetActiveState(bool isActive)
    {
        if (image == null)
        {
            Debug.LogWarning("[SettingsPopUp] SetActiveState - image is null, skipping visual change.");
            return;
        }

        Debug.Log($"[SettingsPopUp] SetActiveState - isActive={isActive}");

        if (activeSprite != null && inactiveSprite != null)
        {
            image.sprite = isActive ? activeSprite : inactiveSprite;
        }
        else
        {
            // ja nav sprite, mainâm krâsu kâ alternatîvu
            image.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
    }
}