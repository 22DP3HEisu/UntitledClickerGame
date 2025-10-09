using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LoginCheckManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    
    [Header("Auto Login Settings")]
    [SerializeField] private float delayBeforeCheck = 0.5f; // Small delay for smooth transition
    
    private void Start()
    {
        // You can call CheckLoginStatus() here if you want automatic checking
        // CheckLoginStatus();
    }
    
    public void CheckLoginStatus()
    {
        if (IsPlayerLoggedIn())
        {
            Debug.Log("Player is already logged in. Redirecting to game...");
            LoadGameScene();
        }
        else
        {
            Debug.Log("Player is not logged in. Staying on intro scene.");
            // If we're not in the intro scene, go there
            if (SceneManager.GetActiveScene().name != introSceneName)
            {
                LoadIntroScene();
            }
        }
    }
    
    public bool IsPlayerLoggedIn()
    {
        // Check if user is registered
        bool isRegistered = PlayerPrefs.GetInt("IsRegistered", 0) == 1;
        if (!isRegistered)
        {
            Debug.Log("User is not registered.");
            return false;
        }
        
        // Use ApiClient's centralized token validation
        if (!ApiClient.IsTokenValid())
        {
            Debug.Log("Auth token is invalid or expired.");
            ClearExpiredUserData();
            return false;
        }
        
        // Check if we have essential user data
        string username = PlayerPrefs.GetString("RegisteredUsername", "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("Essential user data is missing.");
            return false;
        }
        
        Debug.Log($"User {username} is logged in with valid token.");
        return true;
    }
    
    private void ClearExpiredUserData()
    {
        // Use ApiClient's centralized token management
        ApiClient.ClearAuthToken();
        Debug.Log("Cleared expired authentication data.");
    }
    
    public void ForceLogout()
    {
        // Clear all user data
        PlayerPrefs.DeleteKey("RegisteredUsername");
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.DeleteKey("IsRegistered");
        PlayerPrefs.Save();
        
        // Use ApiClient's centralized token management
        ApiClient.ClearAuthToken();
        
        Debug.Log("User logged out. All data cleared.");
        
        // Redirect to intro scene
        LoadIntroScene();
    }
    
    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
    private void LoadIntroScene()
    {
        SceneManager.LoadScene(introSceneName);
    }
    
    // Public method to manually trigger login check (useful for testing)
    [ContextMenu("Check Login Status")]
    public void ManualLoginCheck()
    {
        CheckLoginStatus();
    }
    
    // Public method to get current user info
    public UserInfo GetCurrentUserInfo()
    {
        if (!IsPlayerLoggedIn())
        {
            return null;
        }
        
        return new UserInfo
        {
            username = PlayerPrefs.GetString("RegisteredUsername", ""),
            email = PlayerPrefs.GetString("RegisteredEmail", ""),
            authToken = AuthTokenManager.GetToken()
        };
    }
}

[System.Serializable]
public class UserInfo
{
    public string username;
    public string email;
    public string authToken;
}