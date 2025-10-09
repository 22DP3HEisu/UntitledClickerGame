using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using System;
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
    
    [Header("Validation Settings")]
    [SerializeField] private int minPasswordLength = 6;
    
    [Header("Backend Settings")]
    [SerializeField] private string backendUrl = "http://92.5.105.149:3000"; // Adjust this to your backend URL
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "game";
    [SerializeField] private string introSceneName = "Intro";
    [SerializeField] private string registerSceneName = "Register";
    
    void Start()
    {
        SetupButtons();
        ClearMessages();
        
        // Auto-fill username if we have registration data
        AutoFillUserData();
    }

    private void SetupButtons()
    {
        // Set up login button
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        else
        {
            Debug.LogWarning("Login button is not assigned!");
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
        
        // Set up Enter key support
        if (passwordField != null)
        {
            passwordField.onEndEdit.AddListener(OnPasswordFieldEndEdit);
        }
    }
    
    private void AutoFillUserData()
    {
        // If user has registration data, auto-fill the username field
        string registeredUsername = PlayerPrefs.GetString("RegisteredUsername", "");
        if (!string.IsNullOrEmpty(registeredUsername) && usernameOrEmailField != null)
        {
            usernameOrEmailField.text = registeredUsername;
            Debug.Log("Auto-filled username from registration data.");
        }
    }
    
    private void OnPasswordFieldEndEdit(string value)
    {
        // Allow Enter key to trigger login
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnLoginButtonClicked();
        }
    }

    private void OnLoginButtonClicked()
    {
        ClearMessages();
        
        if (ValidateLoginForm())
        {
            ProcessLogin();
        }
    }
    
    private void OnBackButtonClicked()
    {
        // Go back to the intro scene
        SceneManager.LoadScene(introSceneName);
    }

    private bool ValidateLoginForm()
    {
        string usernameOrEmail = usernameOrEmailField?.text ?? "";
        string password = passwordField?.text ?? "";

        // Validate username/email
        if (string.IsNullOrEmpty(usernameOrEmail))
        {
            ShowErrorMessage("Username or email is required.");
            return false;
        }
        
        if (usernameOrEmail.Length < 3)
        {
            ShowErrorMessage("Username or email is too short.");
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

        return true;
    }

    private void ProcessLogin()
    {
        string usernameOrEmail = usernameOrEmailField.text.Trim();
        string password = passwordField.text;

        Debug.Log($"Processing login for user: {usernameOrEmail}");

        // Send login data to backend
        StartCoroutine(LoginWithBackend(usernameOrEmail, password));
    }

    private IEnumerator LoginWithBackend(string usernameOrEmail, string password)
    {
        // Disable the login button to prevent multiple submissions
        if (loginButton != null)
            loginButton.interactable = false;

        ShowSuccessMessage("Logging in...");

        // Create login data
        var loginData = new LoginData
        {
            username = usernameOrEmail, // Backend accepts username or email in username field
            password = password
        };

        string jsonData = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest($"{backendUrl}/auth/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Re-enable the login button
            if (loginButton != null)
                loginButton.interactable = true;

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                bool parseSuccess = false;
                try
                {
                    var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                    
                    ShowSuccessMessage("Login successful! Loading game...");
                    
                    // Save user data locally
                    SaveUserData(response.user.username, response.user.email, response.token);
                    
                    parseSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing login response: {e.Message}");
                    ShowErrorMessage("Login completed but there was an error processing the response.");
                }
                
                // Only redirect if parsing was successful (yield outside try-catch)
                if (parseSuccess)
                {
                    // Wait a moment then redirect
                    yield return new WaitForSeconds(1.5f);
                    
                    // Redirect to game scene
                    SceneManager.LoadScene(gameSceneName);
                }
            }
            else
            {
                // Handle different error types
                HandleLoginError(request);
            }
        }
    }

    private void HandleLoginError(UnityWebRequest request)
    {
        string errorMessage = "Login failed. Please try again.";

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
                if (request.responseCode == 401) // Unauthorized - invalid credentials
                {
                    errorMessage = "Invalid username/email or password. Please check your credentials.";
                }
                else if (request.responseCode == 400) // Bad request - validation error
                {
                    errorMessage = errorResponse.message ?? "Invalid input. Please check your information.";
                }
                else if (request.responseCode == 404) // User not found
                {
                    errorMessage = "Account not found. Please check your credentials or register a new account.";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing error response: {e.Message}");
                errorMessage = $"Login failed with error code: {request.responseCode}";
            }
            
            Debug.LogError($"Protocol Error: {request.responseCode} - {request.downloadHandler.text}");
        }

        ShowErrorMessage(errorMessage);
    }

    private void SaveUserData(string username, string email, string token)
    {
        // Save user data locally using PlayerPrefs
        PlayerPrefs.SetString("RegisteredUsername", username);
        PlayerPrefs.SetString("RegisteredEmail", email);
        PlayerPrefs.SetInt("IsRegistered", 1);
        
        // Save authentication token
        if (!string.IsNullOrEmpty(token))
        {
            PlayerPrefs.SetString("AuthToken", token);
            
            // Set token expiry (7 days from now, matching backend)
            DateTime expiry = DateTime.Now.AddDays(7);
            PlayerPrefs.SetString("TokenExpiry", expiry.ToBinary().ToString());
        }
        
        PlayerPrefs.Save();
        
        Debug.Log($"User data saved: {username}, {email}");
        Debug.Log("User is now logged in and will skip intro on next app launch.");
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
        
        Debug.LogWarning($"Login Error: {message}");
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
        
        Debug.Log($"Login Success: {message}");
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
        if (loginButton != null)
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);
            
        if (passwordField != null)
            passwordField.onEndEdit.RemoveListener(OnPasswordFieldEndEdit);
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