using UnityEngine;
using UnityEngine.UI;

public class GoToAchievements : MonoBehaviour
{
    public Button openButton;
    public GameObject panel;
    public Button backButton;

    private void Start()
    {
        // Sākumā panelis paslēpts
        if (panel != null)
            panel.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if (backButton != null)
            backButton.onClick.AddListener(ClosePanel);
    }

    private void OpenPanel()
    {
        Debug.Log("OpenPanel pressed");
        if (panel == null)
        {
            Debug.LogError("Panel reference missing!");
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    private void ClosePanel()
    {
        Debug.Log("ClosePanel pressed");
        if (panel == null) return;

        panel.SetActive(false);
    }
}