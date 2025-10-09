using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsPopUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Transform SettingsPanel;
    [SerializeField] private int pageNumber;

    // msg controls whether clicking opens the settings panel (1 = enabled)
    [SerializeField] private int msg = 0;

    private Vector3 startScale;

    private Sprite originalSprite;
    private Color originalColor;

    // canvas/raycaster used to keep popup above the panel without changing hierarchy
    private Canvas popupCanvas;
    private bool hadCanvas;
    private bool createdCanvas;
    private int originalSortingOrder;
    private bool originalOverrideSorting;

    private GraphicRaycaster popupGraphicRaycaster;
    private bool createdGraphicRaycaster;

    private void Awake()
    {
        startScale = transform.localScale;


        // Ensure settings panel starts closed
        if (SettingsPanel != null)
            SettingsPanel.gameObject.SetActive(false);

        // cache canvas state so we can restore it on close
        popupCanvas = GetComponent<Canvas>();
        hadCanvas = popupCanvas != null;
        if (hadCanvas)
        {
            originalSortingOrder = popupCanvas.sortingOrder;
            originalOverrideSorting = popupCanvas.overrideSorting;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float duration = 0.1f;

        LeanTween.scale(gameObject, startScale * 0.9f, duration)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, startScale, duration).setEaseInQuad();

                if (msg == 1)
                    ToggleSettingsPanel();
            });
    }

    private void ToggleSettingsPanel()
    {
        if (SettingsPanel == null)
            return;

        bool isActive = SettingsPanel.gameObject.activeSelf;
        if (isActive)
        {
            // Close panel
            SettingsPanel.gameObject.SetActive(false);

            // Destroy raycaster we created (GraphicRaycaster depends on Canvas)
            if (createdGraphicRaycaster && popupGraphicRaycaster != null)
            {
                Destroy(popupGraphicRaycaster);
                popupGraphicRaycaster = null;
                createdGraphicRaycaster = false;
            }

            // If we created the Canvas, destroy it; otherwise restore original settings
            if (createdCanvas && popupCanvas != null)
            {
                Destroy(popupCanvas);
                popupCanvas = null;
                createdCanvas = false;
            }
            else if (popupCanvas != null && hadCanvas)
            {
                popupCanvas.overrideSorting = originalOverrideSorting;
                popupCanvas.sortingOrder = originalSortingOrder;
            }
        }
        else
        {
            // Open panel
            SettingsPanel.gameObject.SetActive(true);

            // Determine a sorting order that ensures popup renders above the panel
            int targetOrder = 1000;
            Canvas panelCanvas = SettingsPanel.GetComponentInParent<Canvas>();
            if (panelCanvas != null)
                targetOrder = panelCanvas.sortingOrder + 1;

            // Ensure we have a Canvas on the popup and set it to render above the panel.
            popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null)
            {
                popupCanvas = gameObject.AddComponent<Canvas>();
                createdCanvas = true;
                // if popup had no canvas before, no need to restore previous values
                originalSortingOrder = 0;
                originalOverrideSorting = false;
            }
            else
            {
                createdCanvas = false;
                // if it existed, originalSortingOrder/originalOverrideSorting were cached in Awake
            }

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = targetOrder;

            // Ensure there's a GraphicRaycaster so this Canvas can receive clicks.
            popupGraphicRaycaster = GetComponent<GraphicRaycaster>();
            if (popupGraphicRaycaster == null)
            {
                popupGraphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
                createdGraphicRaycaster = true;
            }
            else
            {
                createdGraphicRaycaster = false;
            }
        }

    }

    private void OnDisable()
    {
        // Clean up any components we created
        if (createdGraphicRaycaster && popupGraphicRaycaster != null)
        {
            Destroy(popupGraphicRaycaster);
            popupGraphicRaycaster = null;
            createdGraphicRaycaster = false;
        }

        if (createdCanvas && popupCanvas != null)
        {
            Destroy(popupCanvas);
            popupCanvas = null;
            createdCanvas = false;
        }
        else if (popupCanvas != null && hadCanvas)
        {
            popupCanvas.overrideSorting = originalOverrideSorting;
            popupCanvas.sortingOrder = originalSortingOrder;
        }
    }
}