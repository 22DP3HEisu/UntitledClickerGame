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
    
    void Start()
    {

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
        
        // Check if we have a valid auth token
        string authToken = PlayerPrefs.GetString("AuthToken", "");
        if (string.IsNullOrEmpty(authToken))
        {
            Debug.Log("No auth token found.");
            return false;
        }
        
        // Check if token is expired
        if (IsTokenExpired())
        {
            Debug.Log("Auth token is expired.");
            ClearExpiredUserData();
            return false;
        }
        
        // Check if we have essential user data
        string username = PlayerPrefs.GetString("RegisteredUsername", "");
        string email = PlayerPrefs.GetString("RegisteredEmail", "");
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
        {
            Debug.Log("Essential user data is missing.");
            return false;
        }
        
        Debug.Log($"User {username} is logged in with valid token.");
        return true;
    }
    
    private bool IsTokenExpired()
    {
        string tokenExpiryString = PlayerPrefs.GetString("TokenExpiry", "");
        
        if (string.IsNullOrEmpty(tokenExpiryString))
        {
            return true; // No expiry data means expired
        }
        
        try
        {
            long tokenExpiryBinary = Convert.ToInt64(tokenExpiryString);
            DateTime tokenExpiry = DateTime.FromBinary(tokenExpiryBinary);
            
            return DateTime.Now > tokenExpiry;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing token expiry: {e.Message}");
            return true; // If we can't parse, consider it expired
        }
    }
    
    private void ClearExpiredUserData()
    {
        // Clear expired authentication data but keep registration info
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("TokenExpiry");
        PlayerPrefs.Save();
        
        Debug.Log("Cleared expired authentication data.");
    }
    
    public void ForceLogout()
    {
        // Clear all user data
        PlayerPrefs.DeleteKey("RegisteredUsername");
        PlayerPrefs.DeleteKey("RegisteredEmail");
        PlayerPrefs.DeleteKey("IsRegistered");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("TokenExpiry");
        PlayerPrefs.Save();
        
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
            authToken = PlayerPrefs.GetString("AuthToken", "")
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