using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserLogoutController : MonoBehaviour
{
    [Header("Assign the logout button here")]
    [SerializeField] private Button logoutButton;

    [Header("Intro Scene Name")]
    [SerializeField] private string introSceneName = "Intro";

    private void Awake()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        else
        {
            Debug.LogWarning("Logout button is not assigned in the inspector.");
        }
    }

    private void OnLogoutClicked()
    {
        // Clear user-related PlayerPrefs
        PlayerPrefs.DeleteKey("RegisteredUsername");
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.DeleteKey("IsRegistered");
        PlayerPrefs.Save();

        Debug.Log("User logged out. PlayerPrefs cleared.");

        // Optionally, load the intro scene
        SceneManager.LoadScene(introSceneName);
    }
}