using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GoToAchievments : MonoBehaviour
{
    [Header("UI")]
    public Button Achievments;
    public GameObject AchievmentsPanel;
    public Button Back;

    private CanvasGroup panelCanvasGroup;
    private Image panelBlockerImage;

    private void Awake()
    {
        if (AchievmentsPanel != null)
        {
            // ensure CanvasGroup exists (used to toggle interactability)
            panelCanvasGroup = AchievmentsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = AchievmentsPanel.AddComponent<CanvasGroup>();

            // ensure there's a Graphic on the panel itself to catch raycasts (blocks clicks behind panel)
            panelBlockerImage = AchievmentsPanel.GetComponent<Image>();
            if (panelBlockerImage == null)
            {
                panelBlockerImage = AchievmentsPanel.AddComponent<Image>();
                // Make the blocker transparent visually but still able to receive raycasts
                panelBlockerImage.color = new Color(0f, 0f, 0f, 0f);
            }
            panelBlockerImage.raycastTarget = true;
            panelBlockerImage.enabled = false; // disabled until panel shown
        }
    }

    private void Start()
    {
        if (Achievments != null)
            Achievments.onClick.AddListener(ShowAchievementsPanel);

        if (Back != null)
            Back.onClick.AddListener(HideAchievementsPanel);

        // keep panel hidden at start
        HideAchievementsPanel();
    }

    private void ShowAchievementsPanel()
    {
        if (AchievmentsPanel == null) return;

        // show panel and bring to front
        AchievmentsPanel.SetActive(true);
        AchievmentsPanel.transform.SetAsLastSibling();

        // enable blocker (receives raycasts so clicks don't go through)
        if (panelBlockerImage != null)
        {
            panelBlockerImage.enabled = true;
            panelBlockerImage.raycastTarget = true;
        }

        // allow interactions inside the panel
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }

        // ensure Back button is on top inside the panel so it still receives clicks
        if (Back != null)
        {
            Back.transform.SetAsLastSibling();
            Back.interactable = true;
            // ensure its Image accepts raycasts
            var backImg = Back.GetComponent<Image>();
            if (backImg != null) backImg.raycastTarget = true;
        }
    }

    private void HideAchievementsPanel()
    {
        if (AchievmentsPanel == null) return;

        // disable interactions
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // disable blocker visual/raycast
        if (panelBlockerImage != null)
        {
            panelBlockerImage.enabled = false;
            panelBlockerImage.raycastTarget = false;
        }

        AchievmentsPanel.SetActive(false);
    }
}