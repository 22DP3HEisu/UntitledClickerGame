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
    [Header("Scene Configuration")]
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    
    [Header("Startup Settings")]
    [SerializeField] private bool showSplashScreen = true;
    [SerializeField] private float splashDuration = 2f;
    [SerializeField] private GameObject splashScreenUI;
    
    [Header("Debug Options")]
    [SerializeField] private bool forceShowIntro = false; // For testing purposes
    
    private LoginCheckManager loginCheckManager;
    
    void Start()
    {
        // Initialize login check manager
        loginCheckManager = gameObject.AddComponent<LoginCheckManager>();
        
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
    
    private void StartLoginCheck()
    {
        if (splashScreenUI != null)
        {
            splashScreenUI.SetActive(false);
        }
        
    // Check login status (use async Task instead of coroutine)
    _ = PerformStartupLoginVerification();
    }

    private async System.Threading.Tasks.Task PerformStartupLoginVerification()
    {
        // If loginCheckManager says logged in based on local checks, attempt server verification
        if (loginCheckManager.IsPlayerLoggedIn())
        {
            try
            {
                var resp = await ApiClient.GetAsync<UserProfileResponse>("/user");

                if (resp != null && resp.user != null)
                {
                    // Update saved user info on main thread
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        PlayerPrefs.SetString("RegisteredUsername", resp.user.username ?? PlayerPrefs.GetString("RegisteredUsername", ""));
                        PlayerPrefs.SetString("RegisteredEmail", resp.user.email ?? PlayerPrefs.GetString("RegisteredEmail", ""));
                        PlayerPrefs.Save();
                    });
                }

                Debug.Log($"Server verification succeeded. Welcome back, {resp.user?.username}!");
                // Continue with normal flow (show welcome and load game)
                PerformLoginCheck();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Token verification failed: {ex.Message}");
                // Clear local auth data and go to intro
                PlayerPrefs.DeleteKey("AuthToken");
                PlayerPrefs.DeleteKey("TokenExpiry");
                PlayerPrefs.Save();

                PerformLoginCheck();
            }
        }
        else
        {
            // Not logged in locally — proceed with normal check which will route to intro
            PerformLoginCheck();
        }
    }
    
    private void PerformLoginCheck()
    {
        // Force show intro for testing
        Debug.Log(forceShowIntro);
        if (forceShowIntro)
        {
            Debug.Log("Force show intro is enabled. Going to intro scene.");
            LoadIntroScene();
            return;
        }
        
        // Check if user is logged in
        if (loginCheckManager.IsPlayerLoggedIn())
        {
            UserInfo userInfo = loginCheckManager.GetCurrentUserInfo();
            Debug.Log($"Welcome back, {userInfo.username}! Loading game...");
            
            // Optional: Show a "Welcome back" message briefly
            ShowWelcomeBack(userInfo.username);
        }
        else
        {
            Debug.Log("No valid login found. Showing intro scene.");
            LoadIntroScene();
        }
    }
    
    private void ShowWelcomeBack(string username)
    {
        // You could show a welcome back message here
        Debug.Log($"Welcome back, {username}!");
        
        // Small delay then load game
        Invoke(nameof(LoadGameScene), 1f);
    }
    
    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void LoadIntroScene()
    {
        SceneManager.LoadScene(introSceneName);
    }
    
    // Public methods for manual control
    public void ForceGoToIntro()
    {
        LoadIntroScene();
    }
    
    public void ForceGoToGame()
    {
        LoadGameScene();
    }
    
    public void ForceLogout()
    {
        if (loginCheckManager != null)
        {
            loginCheckManager.ForceLogout();
        }
    }
    
    // Debug method accessible from inspector
    [ContextMenu("Test Login Check")]
    public void TestLoginCheck()
    {
        PerformLoginCheck();
    }
    
    // Debug method to clear all user data
    [ContextMenu("Clear All User Data")]
    public void ClearAllUserData()
    {
        PlayerPrefs.DeleteKey("RegisteredUsername");
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.DeleteKey("IsRegistered");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("TokenExpiry");
        PlayerPrefs.Save();
        
        Debug.Log("All user data cleared! You can now test registration/login again.");
    }
    
    // Debug method to clear only auth token (keep registration data)
    [ContextMenu("Clear Auth Token Only")]
    public void ClearAuthTokenOnly()
    {
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("TokenExpiry");
        PlayerPrefs.Save();
        
        Debug.Log("Auth token cleared! User will need to login again but registration data is kept.");
    }
}