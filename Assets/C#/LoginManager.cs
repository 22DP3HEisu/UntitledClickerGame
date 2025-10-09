using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameOrEmailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text successMessageText;
    
    [Header("Settings")]
    [SerializeField] private int minPasswordLength = 6;
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    
    private void Start()
    {
        SetupUI();
        AutoFillUsername();
    }

    private void SetupUI()
    {
        // Setup button listeners
        loginButton?.onClick.AddListener(OnLoginClicked);
        backButton?.onClick.AddListener(OnBackClicked);
        
        // Enable Enter key to submit
        passwordField?.onEndEdit.AddListener(OnPasswordSubmit);
        
        ClearMessages();
    }
    
    private void AutoFillUsername()
    {
        string savedUsername = PlayerPrefs.GetString("RegisteredUsername", "");
        if (!string.IsNullOrEmpty(savedUsername) && usernameOrEmailField != null)
        {
            usernameOrEmailField.text = savedUsername;
        }
    }
    
    private void OnPasswordSubmit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnLoginClicked();
        }
    }

    private async void OnLoginClicked()
    {
        if (!ValidateInput()) return;
        
        await ProcessLoginAsync();
    }
    
    private void OnBackClicked()
    {
        SceneManager.LoadScene(introSceneName);
    }

    private bool ValidateInput()
    {
        string username = usernameOrEmailField?.text?.Trim() ?? "";
        string password = passwordField?.text ?? "";

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Username or email is required.", isError: true);
            return false;
        }
        
        if (username.Length < 3)
        {
            ShowMessage("Username or email is too short.", isError: true);
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("Password is required.", isError: true);
            return false;
        }
        
        if (password.Length < minPasswordLength)
        {
            ShowMessage($"Password must be at least {minPasswordLength} characters long.", isError: true);
            return false;
        }

        return true;
    }

    private async Task ProcessLoginAsync()
    {
        SetButtonState(enabled: false);
        ShowMessage("Logging in...", isError: false);

        var loginData = new LoginData 
        { 
            username = usernameOrEmailField.text.Trim(), 
            password = passwordField.text 
        };

        try
        {
            var response = await ApiClient.PostAsync<LoginData, LoginResponse>("/auth/login", loginData);

            ShowMessage("Login successful! Loading game...", isError: false);

            // Save user data and set auth token with 7-day expiry
            SaveUserData(response.user.username, response.user.email);
            ApiClient.SetAuthToken(response.token, DateTime.UtcNow.AddDays(7));

            // Brief delay for user feedback, then load game
            await Task.Delay(1500);
            SceneManager.LoadScene(gameSceneName);
        }
        catch (ApiException ex)
        {
            string message = ex.StatusCode switch
            {
                401 => "Invalid username/email or password.",
                404 => "Account not found. Please check your credentials.",
                _ => ex.Message
            };
            ShowMessage(message, isError: true);
        }
        catch (Exception ex)
        {
            ShowMessage("Login failed. Please try again.", isError: true);
            Debug.LogError($"Login error: {ex}");
        }
        finally
        {
            SetButtonState(enabled: true);
        }
    }

    private void SaveUserData(string username, string email)
    {
        PlayerPrefs.SetString("RegisteredUsername", username);
        PlayerPrefs.SetString("RegisteredEmail", email);
        PlayerPrefs.SetInt("IsRegistered", 1);
        PlayerPrefs.Save();
        
        Debug.Log($"User data saved: {username}");
    }

    private void ShowMessage(string message, bool isError)
    {
        ClearMessages();
        
        if (isError && errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
        }
        else if (!isError && successMessageText != null)
        {
            successMessageText.text = message;
            successMessageText.gameObject.SetActive(true);
        }
    }

    private void ClearMessages()
    {
        errorMessageText?.gameObject.SetActive(false);
        successMessageText?.gameObject.SetActive(false);
    }

    private void SetButtonState(bool enabled)
    {
        if (loginButton != null)
            loginButton.interactable = enabled;
    }
    
    private void OnDestroy()
    {
        loginButton?.onClick.RemoveListener(OnLoginClicked);
        backButton?.onClick.RemoveListener(OnBackClicked);
        passwordField?.onEndEdit.RemoveListener(OnPasswordSubmit);
    }
}

// Data classes for JSON serialization/deserialization
[System.Serializable]
public class LoginData
{
    public string username; // Can be username or email
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string message;
    public UserData user;
    public string token;
    public string expiresIn;
}

[System.Serializable]
public class UserData
{
    public int id;
    public string username;
    public string email;
}