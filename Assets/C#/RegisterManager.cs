using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using TMPro;

public class RegisterManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text errorMessageText;
    [SerializeField] private TMP_Text successMessageText;
    
    [Header("Settings")]
    [SerializeField] private int minUsernameLength = 3;
    [SerializeField] private int maxUsernameLength = 30;
    [SerializeField] private int minPasswordLength = 6;
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    
    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        registerButton?.onClick.AddListener(OnRegisterClicked);
        backButton?.onClick.AddListener(OnBackClicked);
        ClearMessages();
    }

    private async void OnRegisterClicked()
    {
        if (!ValidateInput()) return;
        
        await ProcessRegistrationAsync();
    }
    
    private void OnBackClicked()
    {
        SceneManager.LoadScene(introSceneName);
    }

    private bool ValidateInput()
    {
        string username = usernameField?.text?.Trim() ?? "";
        string email = emailField?.text?.Trim() ?? "";
        string password = passwordField?.text ?? "";

        // Username validation
        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Username is required.", isError: true);
            return false;
        }
        
        if (username.Length < minUsernameLength || username.Length > maxUsernameLength)
        {
            ShowMessage($"Username must be between {minUsernameLength} and {maxUsernameLength} characters.", isError: true);
            return false;
        }
        
        if (!IsValidUsername(username))
        {
            ShowMessage("Username can only contain letters, numbers, underscores, and hyphens.", isError: true);
            return false;
        }

        // Email validation
        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("Email is required.", isError: true);
            return false;
        }
        
        if (!IsValidEmail(email))
        {
            ShowMessage("Please enter a valid email address.", isError: true);
            return false;
        }

        // Password validation
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
        
        if (!Regex.IsMatch(password, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{6,}$"))
        {
            ShowMessage("Password must contain at least one letter and one number.", isError: true);
            return false;
        }

        return true;
    }

    private bool IsValidUsername(string username)
    {
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$");
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async Task ProcessRegistrationAsync()
    {
        SetButtonState(enabled: false);
        ShowMessage("Creating account...", isError: false);

        var registrationData = new RegistrationData
        {
            username = usernameField.text.Trim(),
            email = emailField.text.Trim(),
            password = passwordField.text
        };

        try
        {
            var response = await ApiClient.PostAsync<RegistrationData, RegistrationResponse>("/auth/register", registrationData);

            ShowMessage("Account created successfully! Loading game...", isError: false);

            // Save user data and set auth token with 7-day expiry
            SaveUserData(response.user.username, response.user.email);
            ApiClient.SetAuthToken(response.token, DateTime.UtcNow.AddDays(7));

            // Brief delay for user feedback, then load game
            await Task.Delay(2000);
            SceneManager.LoadScene(gameSceneName);
        }
        catch (ApiException ex)
        {
            string message = ex.StatusCode switch
            {
                409 => "Username or email already taken. Please choose different credentials.",
                400 => ex.Message,
                _ => ex.Message
            };
            ShowMessage(message, isError: true);
        }
        catch (Exception ex)
        {
            ShowMessage("Registration failed. Please try again.", isError: true);
            Debug.LogError($"Registration error: {ex}");
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
        if (registerButton != null)
            registerButton.interactable = enabled;
    }
    
    private void OnDestroy()
    {
        registerButton?.onClick.RemoveListener(OnRegisterClicked);
        backButton?.onClick.RemoveListener(OnBackClicked);
    }
}

// Data classes for JSON serialization/deserialization
[System.Serializable]
public class RegistrationData
{
    public string username;
    public string email;
    public string password;
}

[System.Serializable]
public class RegistrationResponse
{
    public string message;
    public UserData user;
    public string token;
    public string expiresIn;
}
