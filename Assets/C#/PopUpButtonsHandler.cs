using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopUpButtonsHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button adminButton;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        // Setup admin button
        if (adminButton != null)
        {
            adminButton.onClick.AddListener(GoToAdminScene);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Navigate to the admin scene
    /// </summary>
    public void GoToAdminScene()
    {
        Debug.Log("Navigating to admin scene...");
        SceneManager.LoadScene("Admin");
    }
}
