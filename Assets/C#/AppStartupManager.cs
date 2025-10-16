using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Startup manager that should be placed on the first scene that loads when the app starts.
/// This can be a splash screen, loading screen, or even the intro scene itself.
/// It will check if the user is logged in and redirect accordingly.
/// </summary>
public class AppStartupManager : MonoBehaviour
{
    #region Inspector Settings
    
    [Header("Scene Configuration")]
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    
    [Header("Startup Settings")]
    [SerializeField] private bool showSplashScreen = true;
    [SerializeField] private float splashDuration = 2f;
    [SerializeField] private GameObject splashScreenUI;
    
    [Header("Debug Options")]
    [SerializeField] private bool forceShowIntro = false; // For testing purposes
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        // Configure UserManager with scene names
        UserManager.SetSceneNames(gameSceneName, introSceneName);
        
        if (showSplashScreen && splashScreenUI != null)
        {
            ShowSplashScreen();
        }
        else
        {
            StartLoginCheck();
        }
    }
    
    private void ShowSplashScreen()
    {
        if (splashScreenUI != null)
        {
            splashScreenUI.SetActive(true);
        }
        
        // Wait for splash duration then check login
        Invoke(nameof(StartLoginCheck), splashDuration);
    }
    
    private async void StartLoginCheck()
    {
        if (splashScreenUI != null)
        {
            splashScreenUI.SetActive(false);
        }
        
        // Perform startup verification
        await PerformStartupLoginVerification();
    }
    
    #endregion
    
    #region Startup Logic

    private async System.Threading.Tasks.Task PerformStartupLoginVerification()
    {
        // If user appears logged in locally, verify with server
        if (UserManager.IsPlayerLoggedIn())
        {
            try
            {
                var response = await ApiClient.GetAsync<UserProfileResponse>("/user");

                if (response?.user != null)
                {
                    // Update saved user info
                    PlayerPrefs.SetString("RegisteredUsername", response.user.username ?? PlayerPrefs.GetString("RegisteredUsername", ""));
                    PlayerPrefs.SetString("RegisteredEmail", response.user.email ?? PlayerPrefs.GetString("RegisteredEmail", ""));
                    PlayerPrefs.Save();

                    Debug.Log($"Server verification succeeded. Welcome back, {response.user.username}!");
                }

                PerformLoginCheck();
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"Token verification failed: {ex.Message}");
                // ApiClient will handle token clearing automatically
                PerformLoginCheck();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Server verification failed: {ex.Message}");
                PerformLoginCheck();
            }
        }
        else
        {
            PerformLoginCheck();
        }
    }
    
    private void PerformLoginCheck()
    {
        // Force show intro for testing
        if (forceShowIntro)
        {
            Debug.Log("Force show intro is enabled. Going to intro scene.");
            UserManager.LoadIntroScene();
            return;
        }
        
        // Check if user is logged in and navigate accordingly
        if (UserManager.IsPlayerLoggedIn())
        {
            string username = UserManager.GetCurrentUsername();
            Debug.Log($"Welcome back, {username}! Loading game...");
            ShowWelcomeBack(username);
        }
        else
        {
            Debug.Log("No valid login found. Showing intro scene.");
            UserManager.LoadIntroScene();
        }
    }
    
    private void ShowWelcomeBack(string username)
    {
        // You could show a welcome back message here
        Debug.Log($"Welcome back, {username}!");
        
        // Small delay then load game
        Invoke(nameof(LoadGameSceneDelayed), 1f);
    }
    
    private void LoadGameSceneDelayed()
    {
        UserManager.LoadGameScene();
    }
    
    #endregion
    
    #region Public Interface
    
    // Debug method accessible from inspector
    [ContextMenu("Test Login Check")]
    public void TestLoginCheck()
    {
        PerformLoginCheck();
    }
    
    #endregion
    
    #region Debug Methods
    
    // Debug method to clear all user data
    [ContextMenu("Clear All User Data")]
    public void ClearAllUserData()
    {
        UserManager.ForceLogout(); // This handles all the clearing logic
        Debug.Log("All user data cleared! You can now test registration/login again.");
    }
    
    // Debug method to clear only auth token (keep registration data)
    [ContextMenu("Clear Auth Token Only")]
    public void ClearAuthTokenOnly()
    {
        ApiClient.ClearAuthToken();
        Debug.Log("Auth token cleared! User will need to login again but registration data is kept.");
    }
    
    #endregion
}