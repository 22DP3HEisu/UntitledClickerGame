using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopUpButtonsHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button adminButton;
    [SerializeField] private Button logoutButton;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async Task Start()
    {
        await SetupButtons();
    }

    private async Task SetupButtons()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        try
        {
            var response = await ApiClient.GetAsync<UserProfileResponse>("/user");

            if (response != null && response.user.role == "Admin" && adminButton != null)
            {
                adminButton.gameObject.SetActive(true);
                adminButton.onClick.AddListener(GoToAdminScene);
            }
        }
        catch (ApiException ex)
        {
            Debug.LogError($"[PopUpButtonsHandler] API error loading user profile: {ex.Message}");
        }
    }
    
    private void GoToAdminScene()
    {
        Debug.Log("Navigating to admin scene...");
        SceneManager.LoadScene("Admin");
    }

    private void OnLogoutClicked()
    {
        UserManager.ForceLogout();
    }
}
