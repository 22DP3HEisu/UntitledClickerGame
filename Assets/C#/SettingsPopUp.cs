using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsPopUp : MonoBehaviour, IPointerClickHandler
{
    public enum ButtonType
    {
        Settings,
        Quest
    }

    [Header("Settings")]
    [SerializeField] private Transform SettingsPanel;
    [SerializeField] private int pageNumber;

    [Header("Behavior Settings")]
    [SerializeField] private int msg = 0;

    [Header("Button Type")]
    [SerializeField] private ButtonType buttonType = ButtonType.Settings;

    private Vector3 startScale;
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

        if (SettingsPanel != null)
            SettingsPanel.gameObject.SetActive(false);

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
                    HandleButtonAction();
            });
    }

    /// <summary>
    /// Handles different button actions based on button type.
    /// Quest button requires active token.
    /// </summary>
    private void HandleButtonAction()
    {
        switch (buttonType)
        {
            case ButtonType.Quest:
                TryOpenQuestPanel();
                break;

            case ButtonType.Settings:
                ToggleSettingsPanel();
                break;
        }
    }

    /// <summary>
    /// Only Quest button checks for token before opening.
    /// </summary>
    private void TryOpenQuestPanel()
    {
        string authToken = PlayerPrefs.GetString("AuthToken", "");

        if (string.IsNullOrEmpty(authToken))
        {
            Debug.LogWarning("No active token found. Redirecting to Register scene...");
            SceneManager.LoadScene("Register");
            return;
        }

        ToggleSettingsPanel();
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
        else
        {
            // Open panel
            SettingsPanel.gameObject.SetActive(true);

            int targetOrder = 1000;
            Canvas panelCanvas = SettingsPanel.GetComponentInParent<Canvas>();
            if (panelCanvas != null)
                targetOrder = panelCanvas.sortingOrder + 1;

            popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null)
            {
                popupCanvas = gameObject.AddComponent<Canvas>();
                createdCanvas = true;
                originalSortingOrder = 0;
                originalOverrideSorting = false;
            }
            else
            {
                createdCanvas = false;
            }

            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = targetOrder;

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
