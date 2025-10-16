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
    [SerializeField] private int minPasswordLength = 8; // Increased from 6 for better security
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
        
        // Enhanced password validation with stronger requirements
        if (!IsValidPassword(password))
        {
            ShowMessage("Password must contain at least:\n• 1 uppercase letter (A-Z)\n• 1 lowercase letter (a-z)\n• 1 number (0-9)\n• 1 special character (!@#$%^&*)\n• Minimum 8 characters\n• No common sequences", isError: true);
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

    /// <summary>
    /// Comprehensive password validation with strong security requirements
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets all security criteria</returns>
    private bool IsValidPassword(string password)
    {
        // Check minimum length
        if (password.Length < minPasswordLength)
            return false;

        // Check maximum length (prevent extremely long passwords that could cause DoS)
        if (password.Length > 128)
            return false;

        // Must contain at least one uppercase letter
        if (!Regex.IsMatch(password, @"(?=.*[A-Z])"))
            return false;

        // Must contain at least one lowercase letter
        if (!Regex.IsMatch(password, @"(?=.*[a-z])"))
            return false;

        // Must contain at least one digit
        if (!Regex.IsMatch(password, @"(?=.*\d)"))
            return false;

        // Must contain at least one special character
        if (!Regex.IsMatch(password, @"(?=.*[!@#$%^&*()_+\-=\[\]{}|;':"",./<>?`~])"))
            return false;

        // Prevent common weak patterns
        if (ContainsWeakPatterns(password))
            return false;

        // Prevent common dictionary words (basic check)
        if (ContainsCommonWords(password.ToLower()))
            return false;

        // Check for repetitive characters (e.g., "aaa", "111")
        if (HasExcessiveRepetition(password))
            return false;

        // Check for sequential characters (e.g., "123", "abc")
        if (HasSequentialCharacters(password))
            return false;

        // All checks passed
        return true;
    }

    /// <summary>
    /// Check for weak password patterns
    /// </summary>
    private bool ContainsWeakPatterns(string password)
    {
        string lowerPassword = password.ToLower();
        
        // Check for keyboard patterns
        string[] keyboardPatterns = {
            "qwerty", "asdf", "zxcv", "qwer", "asdfgh", "zxcvbn",
            "123456", "abcdef", "password", "admin", "user", "guest",
            "12345", "54321", "098765", "987654"
        };

        foreach (string pattern in keyboardPatterns)
        {
            if (lowerPassword.Contains(pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check for common dictionary words
    /// </summary>
    private bool ContainsCommonWords(string password)
    {
        string[] commonWords = {
            "password", "admin", "user", "guest", "login", "welcome",
            "hello", "world", "test", "demo", "sample", "example",
            "qwerty", "abc123", "letmein", "monkey", "dragon",
            "sunshine", "princess", "football", "baseball", "superman"
        };

        foreach (string word in commonWords)
        {
            if (password.Contains(word))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check for excessive character repetition
    /// </summary>
    private bool HasExcessiveRepetition(string password)
    {
        int maxRepeats = 2; // Allow maximum 2 consecutive identical characters
        
        for (int i = 0; i < password.Length - maxRepeats; i++)
        {
            char currentChar = password[i];
            int consecutiveCount = 1;
            
            for (int j = i + 1; j < password.Length && password[j] == currentChar; j++)
            {
                consecutiveCount++;
                if (consecutiveCount > maxRepeats)
                    return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Check for sequential characters (ascending or descending)
    /// </summary>
    private bool HasSequentialCharacters(string password)
    {
        int maxSequential = 2; // Allow maximum 2 sequential characters
        
        for (int i = 0; i < password.Length - maxSequential; i++)
        {
            // Check ascending sequence
            bool isAscending = true;
            bool isDescending = true;
            
            for (int j = 1; j <= maxSequential; j++)
            {
                if (i + j >= password.Length)
                    break;
                    
                char current = password[i + j - 1];
                char next = password[i + j];
                
                if (next != current + 1)
                    isAscending = false;
                    
                if (next != current - 1)
                    isDescending = false;
            }
            
            if (isAscending || isDescending)
                return true;
        }
        
        return false;
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
