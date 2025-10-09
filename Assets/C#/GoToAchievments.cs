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

    private void Awake()
    {
        
        // Get or add CanvasGroup component
        if (AchievmentsPanel != null)
        {
            panelCanvasGroup = AchievmentsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = AchievmentsPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        if (Achievments != null)
        {
            Achievments.onClick.AddListener(ShowAchievementsPanel);
        }

        if (Back != null)
        {
            Back.onClick.AddListener(HideAchievementsPanel);
        }

        // Force disable panel at start
        HideAchievementsPanel();
    }

    private void ShowAchievementsPanel()
    {
        if (AchievmentsPanel != null)
        {
            AchievmentsPanel.SetActive(true);
            AchievmentsPanel.transform.SetAsLastSibling(); // Brings panel to front

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true; // Blocks clicks from passing through
            }
        }
    }

    private void HideAchievementsPanel()
    {
        if (AchievmentsPanel != null)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            AchievmentsPanel.SetActive(false);
        }
    }
}