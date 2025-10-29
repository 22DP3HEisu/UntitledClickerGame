using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// Handles the clan creation modal functionality including validation and API calls
/// </summary>
public class ClanCreateModal : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField clanNameInput;
    [SerializeField] private TMP_InputField clanDescriptionInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Validation Settings")]
    [SerializeField] private int minNameLength = 3;
    [SerializeField] private int maxNameLength = 50;
    [SerializeField] private int maxDescriptionLength = 100;
    
    // References
    private ClanManager clanManager;
    
    // Events
    public event Action<ClanData> OnClanCreated;
    public event Action OnClanCreationCancelled;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        SetupUI();
    }
    
    private void OnEnable()
    {
        ClearInputs();
        ShowStatus("", false);
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show the clan creation modal
    /// </summary>
    public void ShowModal(ClanManager manager = null)
    {
        clanManager = manager;
        gameObject.SetActive(true);
        
        // Focus on clan name input
        if (clanNameInput != null)
        {
            clanNameInput.Select();
            clanNameInput.ActivateInputField();
        }
    }
    
    /// <summary>
    /// Hide the clan creation modal
    /// </summary>
    public void HideModal()
    {
        gameObject.SetActive(false);
        ClearInputs();
    }
    
    #endregion
    
    #region UI Setup
    
    private void SetupUI()
    {
        // Setup create button
        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(() => _ = CreateClanAsync());
        }
        
        // Setup cancel button
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelCreation);
        }
        
        // Setup input field validation
        if (clanNameInput != null)
        {
            clanNameInput.onValueChanged.AddListener(ValidateInputs);
            clanNameInput.onSubmit.AddListener((text) => { _ = CreateClanAsync(); });
        }
        
        if (clanDescriptionInput != null)
        {
            clanDescriptionInput.onValueChanged.AddListener(ValidateInputs);
            clanDescriptionInput.onSubmit.AddListener((text) => { _ = CreateClanAsync(); });
        }
        
        // Initial validation
        ValidateInputs("");
    }
    
    #endregion
    
    #region Validation
    
    private void ValidateInputs(string value = "")
    {
        bool isValid = true;
        string errorMessage = "";
        
        // Get current input values
        string clanName = clanNameInput?.text?.Trim() ?? "";
        string clanDescription = clanDescriptionInput?.text?.Trim() ?? "";
        
        // Validate clan name
        if (string.IsNullOrEmpty(clanName))
        {
            isValid = false;
            errorMessage = "Clan name is required";
        }
        else if (clanName.Length < minNameLength)
        {
            isValid = false;
            errorMessage = $"Clan name must be at least {minNameLength} characters";
        }
        else if (clanName.Length > maxNameLength)
        {
            isValid = false;
            errorMessage = $"Clan name cannot exceed {maxNameLength} characters";
        }
        
        // Validate clan description (optional but has max length)
        if (!string.IsNullOrEmpty(clanDescription) && clanDescription.Length > maxDescriptionLength)
        {
            isValid = false;
            errorMessage = $"Clan description cannot exceed {maxDescriptionLength} characters";
        }
        
        // Update UI based on validation
        if (createButton != null)
        {
            createButton.interactable = isValid;
        }
        
        // Show validation error if any
        if (!isValid && !string.IsNullOrEmpty(errorMessage))
        {
            ShowStatus(errorMessage, true);
        }
        else if (isValid)
        {
            ShowStatus("", false);
        }
    }
    
    #endregion
    
    #region Clan Creation
    
    private async Task CreateClanAsync()
    {
        // Validate inputs one more time
        string clanName = clanNameInput?.text?.Trim() ?? "";
        string clanDescription = clanDescriptionInput?.text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(clanName) || clanName.Length < minNameLength)
        {
            ShowStatus("Please enter a valid clan name", true);
            return;
        }
        
        try
        {
            SetLoadingState(true);
            ShowStatus("Creating clan...", false);
            
            // Prepare request data
            var requestData = new CreateClanRequest
            {
                clanName = clanName,
                clanDescription = string.IsNullOrEmpty(clanDescription) ? null : clanDescription
            };
            
            // Send create clan request
            var response = await ApiClient.PostAsync<CreateClanRequest, CreateClanResponse>("/clans", requestData);
            
            if (response?.success == true && response.clan != null)
            {
                ShowStatus("Clan created successfully!", false);
                
                // Trigger events
                OnClanCreated?.Invoke(response.clan);
                
                // Update panel visibility and refresh clan list if manager is available
                if (clanManager != null)
                {
                    // The OnClanCreated event will also be called in ClanManager, 
                    // but we can call this directly for immediate feedback
                    clanManager.OnUserJoinedClan();
                }
                
                // Close modal after a short delay
                await Task.Delay(1000);
                HideModal();
                
                Debug.Log($"Clan created successfully: {response.clan.name} ({response.clan.tag})");
            }
            else
            {
                string errorMsg = response?.message ?? "Failed to create clan";
                ShowStatus(errorMsg, true);
                Debug.LogWarning($"Clan creation failed: {errorMsg}");
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                400 => GetDetailedErrorMessage(ex.Message),
                401 => "Authentication required. Please login.",
                403 => "Permission denied",
                409 => "A clan with this name already exists",
                _ => "Failed to create clan. Please try again."
            };
            
            ShowStatus(errorMessage, true);
            Debug.LogError($"Clan creation API error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Network error. Please check your connection.", true);
            Debug.LogError($"Clan creation error: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }
    
    private string GetDetailedErrorMessage(string apiMessage)
    {
        // Parse common API error messages and make them user-friendly
        if (apiMessage.Contains("already a leader"))
        {
            return "You are already a leader of another clan";
        }
        else if (apiMessage.Contains("already a member"))
        {
            return "You must leave your current clan first";
        }
        else if (apiMessage.Contains("name already exists"))
        {
            return "A clan with this name already exists";
        }
        else if (apiMessage.Contains("name must be"))
        {
            return $"Clan name must be between {minNameLength} and {maxNameLength} characters";
        }
        else if (apiMessage.Contains("description cannot exceed"))
        {
            return $"Description cannot exceed {maxDescriptionLength} characters";
        }
        else
        {
            return "Please check your input and try again";
        }
    }
    
    #endregion
    
    #region UI Actions
    
    private void CancelCreation()
    {
        OnClanCreationCancelled?.Invoke();
        HideModal();
    }
    
    private void ClearInputs()
    {
        if (clanNameInput != null)
        {
            clanNameInput.text = "";
        }
        
        if (clanDescriptionInput != null)
        {
            clanDescriptionInput.text = "";
        }
        
        ValidateInputs();
    }
    
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
    }
    
    private void SetLoadingState(bool loading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(loading);
        }
        
        // Disable input during loading
        if (createButton != null)
        {
            createButton.interactable = !loading;
        }
        
        if (clanNameInput != null)
        {
            clanNameInput.interactable = !loading;
        }
        
        if (clanDescriptionInput != null)
        {
            clanDescriptionInput.interactable = !loading;
        }
    }
    
    #endregion
}

#region Data Structures

[Serializable]
public class CreateClanRequest
{
    public string clanName;
    public string clanDescription;
}

[Serializable]
public class CreateClanResponse
{
    public bool success;
    public string message;
    public ClanData clan;
}

#endregion