using UnityEngine;
using UnityEngine.SceneManagement;
using System;

// Static utility class for managing user authentication and login status checks
// Can be accessed from any script without needing a reference
public static class UserManager
{
    #region Scene Configuration
    
    // Default scene names - can be overridden by calling SetSceneNames()
    private static string gameSceneName = "game";
    private static string introSceneName = "Intro";
    
    // Configure scene names for navigation (call this once during game initialization)
    // <param name="gameScene">Name of the main game scene</param>
    // <param name="introScene">Name of the intro/login scene</param>
    public static void SetSceneNames(string gameScene, string introScene)
    {
        gameSceneName = gameScene;
        introSceneName = introScene;
        Debug.Log($"[UserManager] Scene names set - Game: {gameSceneName}, Intro: {introSceneName}");
    }
    
    #endregion
    
    #region Login Status Management
    
    // Check login status and automatically navigate to appropriate scene
    public static void CheckLoginStatus()
    {
        if (IsPlayerLoggedIn())
        {
            Debug.Log("[UserManager] Player is already logged in. Redirecting to game...");
            LoadGameScene();
        }
        else
        {
            Debug.Log("[UserManager] Player is not logged in. Staying on intro scene.");
            // If we're not in the intro scene, go there
            if (SceneManager.GetActiveScene().name != introSceneName)
            {
                LoadIntroScene();
            }
        }
    }
    
    // Check if the player is currently logged in with valid credentials
    // <returns>True if player is logged in with valid token and data</returns>
    public static bool IsPlayerLoggedIn()
    {
        // Check if user is registered
        bool isRegistered = PlayerPrefs.GetInt("IsRegistered", 0) == 1;
        if (!isRegistered)
        {
            Debug.Log("[UserManager] User is not registered.");
            return false;
        }
        
        // Use ApiClient's centralized token validation
        if (!ApiClient.IsTokenValid())
        {
            Debug.Log("[UserManager] Auth token is invalid or expired.");
            ClearExpiredUserData();
            return false;
        }
        
        // Check if we have essential user data
        string username = PlayerPrefs.GetString("RegisteredUsername", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("[UserManager] Essential user data is missing.");
            return false;
        }
        
        Debug.Log($"[UserManager] User {username} is logged in with valid token.");
        return true;
    }
    
    // Clear expired user data when token is invalid
    private static void ClearExpiredUserData()
    {
        // Use ApiClient's centralized token management
        ApiClient.ClearAuthToken();
        Debug.Log("[UserManager] Cleared expired authentication data.");
    }
    
    #endregion
    
    #region Logout Management
    
    // Force logout the current user and clear all data
    public static void ForceLogout()
    {
        // Clear all user data
        PlayerPrefs.DeleteKey("RegisteredUsername");
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.DeleteKey("IsRegistered");

        PlayerPrefs.DeleteKey("LocalCarrots");
        PlayerPrefs.DeleteKey("LocalHorseShoes");
        PlayerPrefs.DeleteKey("LocalGoldenCarrots");
        PlayerPrefs.Save();
        
        // Use ApiClient's centralized token management
        ApiClient.ClearAuthToken();
        
        Debug.Log("[UserManager] User logged out. All data cleared.");
        
        // Redirect to intro scene
        LoadIntroScene();
    }
    
    #endregion
    
    #region Scene Navigation
    
    // Load the main game scene
    public static void LoadGameScene()
    {
        Debug.Log($"[UserManager] Loading game scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }
    
    // Load the intro/login scene
    public static void LoadIntroScene()
    {
        Debug.Log($"[UserManager] Loading intro scene: {introSceneName}");
        SceneManager.LoadScene(introSceneName);
    }
    
    #endregion
    
    #region User Information
    
    // Get current user information if logged in
    // <returns>UserInfo object or null if not logged in</returns>
    public static UserInfo GetCurrentUserInfo()
    {
        if (!IsPlayerLoggedIn())
        {
            return null;
        }
        
        return new UserInfo
        {
            username = PlayerPrefs.GetString("RegisteredUsername", ""),
            email = PlayerPrefs.GetString("RegisteredEmail", ""),
            authToken = ApiClient.GetAuthToken() // Updated to use ApiClient method
        };
    }
    
    // Get current username (quick access)
    // <returns>Username string or empty if not logged in</returns>
    public static string GetCurrentUsername()
    {
        return IsPlayerLoggedIn() ? PlayerPrefs.GetString("RegisteredUsername", "") : "";
    }
    
    // Get current user email (quick access)
    // <returns>Email string or empty if not logged in</returns>
    public static string GetCurrentUserEmail()
    {
        return IsPlayerLoggedIn() ? PlayerPrefs.GetString("RegisteredEmail", "") : "";
    }
    
    #endregion
    
    #region Validation Utilities
    
    // Check if user has valid authentication token (without full login check)
    // <returns>True if token exists and is valid</returns>
    public static bool HasValidToken()
    {
        return ApiClient.IsTokenValid();
    }
    
    // Check if user registration data exists locally
    // <returns>True if registration data is present</returns>
    public static bool HasRegistrationData()
    {
        bool isRegistered = PlayerPrefs.GetInt("IsRegistered", 0) == 1;
        string username = PlayerPrefs.GetString("RegisteredUsername", "");
        return isRegistered && !string.IsNullOrEmpty(username);
    }
    
    #endregion
}

[System.Serializable]
public class UserInfo
{
    public string username;
    public string email;
    public string authToken;
}