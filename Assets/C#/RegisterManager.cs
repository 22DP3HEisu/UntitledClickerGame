using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using System;
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
    
    [Header("Validation Settings")]
    [SerializeField] private int minUsernameLength = 3;
    [SerializeField] private int maxUsernameLength = 30;
    [SerializeField] private int minPasswordLength = 6;
    
    [Header("Backend Settings")]
    [SerializeField] private string backendUrl = "http://92.5.105.149:3000"; // Adjust this to your backend URL
    
    void Start()
    {
        SetupButtons();
        ClearMessages();
    }

    private void SetupButtons()
    {
        // Set up register button
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        }
        else
        {
            Debug.LogWarning("Register button is not assigned!");
        }
        
        // Set up back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogWarning("Back button is not assigned!");
        }
    }

    private void OnRegisterButtonClicked()
    {
        ClearMessages();
        
        if (ValidateRegistrationForm())
        {
            ProcessRegistration();
        }
    }
    
    private void OnBackButtonClicked()
    {
        // Go back to the intro scene
        SceneManager.LoadScene("Intro");
    }

    private bool ValidateRegistrationForm()
    {
        string username = usernameField?.text ?? "";
        string email = emailField?.text ?? "";
        string password = passwordField?.text ?? "";

        // Validate username
        if (string.IsNullOrEmpty(username))
        {
            ShowErrorMessage("Username is required.");
            return false;
        }
        
        if (username.Length < minUsernameLength || username.Length > maxUsernameLength)
        {
            ShowErrorMessage($"Username must be between {minUsernameLength} and {maxUsernameLength} characters.");
            return false;
        }
        
        if (!IsValidUsername(username))
        {
            ShowErrorMessage("Username can only contain letters, numbers, and underscores.");
            return false;
        }

        // Validate email
        if (string.IsNullOrEmpty(email))
        {
            ShowErrorMessage("Email is required.");
            return false;
        }
        
        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address.");
            return false;
        }

        // Validate password
        if (string.IsNullOrEmpty(password))
        {
            ShowErrorMessage("Password is required.");
            return false;
        }
        
        if (password.Length < minPasswordLength)
        {
            ShowErrorMessage($"Password must be at least {minPasswordLength} characters long.");
            return false;
        }
        
        // Check for at least one letter and one number (matching backend validation)
        if (!Regex.IsMatch(password, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*#?&]{6,}$"))
        {
            ShowErrorMessage("Password must contain at least one letter and one number.");
            return false;
        }

        return true;
    }

    private bool IsValidUsername(string username)
    {
        // Allow letters, numbers, underscores, and hyphens (matching backend validation)
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$");
    }

    private bool IsValidEmail(string email)
    {
        // Basic email validation
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private void ProcessRegistration()
    {
        string username = usernameField.text;
        string email = emailField.text;
        string password = passwordField.text;

        Debug.Log($"Processing registration for user: {username}");

        // Send registration data to backend
        StartCoroutine(RegisterWithBackend(username, email, password));
    }

    private IEnumerator RegisterWithBackend(string username, string email, string password)
    {
        // Disable the register button to prevent multiple submissions
        if (registerButton != null)
            registerButton.interactable = false;

        ShowSuccessMessage("Creating account...");

        // Create registration data
        var registrationData = new RegistrationData
        {
            username = username,
            email = email,
            password = password
        };

        string jsonData = JsonUtility.ToJson(registrationData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest($"{backendUrl}/auth/register", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Re-enable the register button
            if (registerButton != null)
                registerButton.interactable = true;

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                bool parseSuccess = false;
                try
                {
                    var response = JsonUtility.FromJson<RegistrationResponse>(request.downloadHandler.text);
                    
                    ShowSuccessMessage("Account created successfully! Redirecting to game...");
                    
                    // Save user data locally
                    SaveUserData(response.user.username, response.user.email, response.token);
                    
                    parseSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing registration response: {e.Message}");
                    ShowErrorMessage("Registration completed but there was an error processing the response.");
                }
                
                // Only redirect if parsing was successful (yield outside try-catch)
                if (parseSuccess)
                {
                    // Wait a moment then redirect
                    yield return new WaitForSeconds(2f);
                    
                    // Redirect to login scene or main game
                    SceneManager.LoadScene("game");
                }
            }
            else
            {
                // Handle different error types
                HandleRegistrationError(request);
            }
        }
    }

    private void HandleRegistrationError(UnityWebRequest request)
    {
        string errorMessage = "Registration failed. Please try again.";

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            errorMessage = "Cannot connect to server. Please check your internet connection.";
            Debug.LogError($"Connection Error: {request.error}");
        }
        else if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            try
            {
                var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                errorMessage = errorResponse.message ?? errorMessage;
                
                // Handle specific error codes
                if (request.responseCode == 409) // Conflict - user already exists
                {
                    errorMessage = "Username or email already taken. Please choose different credentials.";
                }
                else if (request.responseCode == 400) // Bad request - validation error
                {
                    errorMessage = errorResponse.message ?? "Invalid input. Please check your information.";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing error response: {e.Message}");
                errorMessage = $"Registration failed with error code: {request.responseCode}";
            }
            
            Debug.LogError($"Protocol Error: {request.responseCode} - {request.downloadHandler.text}");
        }

        ShowErrorMessage(errorMessage);
    }

    private void SaveUserData(string username, string email, string token = null)
    {
        // Save user data locally using PlayerPrefs
        PlayerPrefs.SetString("RegisteredUsername", username);
        PlayerPrefs.SetString("RegisteredEmail", email);
        PlayerPrefs.SetInt("IsRegistered", 1);

        // Save authentication token if provided
        if (!string.IsNullOrEmpty(token))
        {
            PlayerPrefs.SetString("AuthToken", token);
            PlayerPrefs.SetString("TokenExpiry", DateTime.Now.AddDays(7).ToBinary().ToString());
        }
        
        PlayerPrefs.Save();
        
        Debug.Log($"User data saved: {username}, {email}");
        Debug.Log("User is now logged in and can skip intro on next app launch.");
    }

    private void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
        }
        
        if (successMessageText != null)
        {
            successMessageText.gameObject.SetActive(false);
        }
        
        Debug.LogWarning($"Registration Error: {message}");
    }

    private void ShowSuccessMessage(string message)
    {
        if (successMessageText != null)
        {
            successMessageText.text = message;
            successMessageText.gameObject.SetActive(true);
        }
        
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
        
        Debug.Log($"Registration Success: {message}");
    }

    private void ClearMessages()
    {
        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);
        
        if (successMessageText != null)
            successMessageText.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (registerButton != null)
            registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);
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

[System.Serializable]
public class UserData
{
    public int id;
    public string username;
    public string email;
}

[System.Serializable]
public class ErrorResponse
{
    public string error;
    public string message;
}
