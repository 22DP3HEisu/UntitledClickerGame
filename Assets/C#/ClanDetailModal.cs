using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Modal window for displaying detailed clan information with comprehensive management functionality
/// Handles all clan operations: viewing, joining, leaving, and member management
/// </summary>
public class ClanDetailModal : MonoBehaviour
{
    [Header("Basic Info Display")]
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanTagText;
    [SerializeField] private TMP_Text clanDescriptionText;
    [SerializeField] private TMP_Text leaderNameText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private TMP_Text creationDateText;
    
    [Header("Status & Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Member Actions")]
    [SerializeField] private Button joinButton;
    [SerializeField] private Button leaveButton;
    
    [Header("Navigation")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;
    
    [Header("Member List (Optional)")]
    [SerializeField] private Transform memberListParent;
    [SerializeField] private GameObject memberItemPrefab;
    
    // State management
    private ClanData currentClan;
    private ClanManager clanManager;
    private UserProfileResponse.UserProfile currentUser;
    private ClanDetailData detailedClanInfo;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        SetupButtons();
        HideAllActionButtons();
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show the modal with clan information and determine available actions
    /// </summary>
    public async void ShowModal(ClanData clan, ClanManager manager = null)
    {
        if (clan == null)
        {
            Debug.LogError("Cannot show modal: clan data is null");
            return;
        }
        
        currentClan = clan;
        clanManager = manager;
        
        gameObject.SetActive(true);
        
        // Show basic clan info immediately
        DisplayBasicClanInfo();
        
        // Load detailed information and determine user actions
        await LoadDetailedClanInfoAsync();
    }
    
    /// <summary>
    /// Legacy method for backward compatibility with ClanManager
    /// </summary>
    public void ShowClanDetails(ClanData clan)
    {
        ShowModal(clan, clanManager);
    }
    
    /// <summary>
    /// Hide the modal
    /// </summary>
    public void HideModal()
    {
        gameObject.SetActive(false);
        ClearClanData();
    }
    
    #endregion
    
    #region UI Setup
    
    private void SetupButtons()
    {
        // Member action buttons
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => _ = JoinClanAsync());
        }
        
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => _ = LeaveClanAsync());
        }
        
        // Navigation buttons
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideModal);
        }
        
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => _ = RefreshClanInfoAsync());
        }
    }
    
    private void HideAllActionButtons()
    {
        SetButtonVisibility(joinButton, false);
        SetButtonVisibility(leaveButton, false);
    }
    
    private void SetButtonVisibility(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }
    
    #endregion
    
    #region Data Loading
    
    private async Task LoadDetailedClanInfoAsync()
    {
        try
        {
            ShowStatus("Loading clan details...", false);
            SetLoadingState(true);
            
            // Load user profile and detailed clan info in parallel
            var userTask = LoadCurrentUserAsync();
            var clanTask = LoadDetailedClanAsync();
            
            await Task.WhenAll(userTask, clanTask);
            
            currentUser = await userTask;
            detailedClanInfo = await clanTask;
            
            if (currentUser != null && detailedClanInfo != null)
            {
                DisplayDetailedClanInfo();
                DetermineAvailableActions();
                ShowStatus("Ready", false);
            }
            else
            {
                ShowStatus("Failed to load clan information", true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading clan details: {ex.Message}");
            ShowStatus("Error loading clan information", true);
        }
        finally
        {
            SetLoadingState(false);
        }
    }
    
    private async Task<UserProfileResponse.UserProfile> LoadCurrentUserAsync()
    {
        try
        {
            var response = await ApiClient.GetAsync<UserProfileResponse>("/user");
            return response?.user;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load user profile: {ex.Message}");
            return null;
        }
    }
    
    private async Task<ClanDetailData> LoadDetailedClanAsync()
    {
        try
        {
            var response = await ApiClient.GetAsync<ClanDetailResponse>($"/clans/{currentClan.id}");
            return response?.success == true ? response.clan : null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load detailed clan info: {ex.Message}");
            return null;
        }
    }
    
    private async Task RefreshClanInfoAsync()
    {
        if (currentClan != null)
        {
            await LoadDetailedClanInfoAsync();
            
            // Refresh the clan list if manager is available
            if (clanManager != null)
            {
                _ = clanManager.LoadClansAsync();
            }
        }
    }
    
    #endregion
    
    #region UI Display
    
    private void DisplayBasicClanInfo()
    {
        SetText(clanNameText, currentClan.name);
        SetText(clanTagText, currentClan.tag);
        SetText(clanDescriptionText, currentClan.description);
        SetText(memberCountText, $"{currentClan.memberCount}/50");
    }
    
    private void DisplayDetailedClanInfo()
    {
        if (detailedClanInfo == null) return;
        
        // Update with detailed information
        SetText(clanNameText, detailedClanInfo.name);
        SetText(clanTagText, detailedClanInfo.tag);
        SetText(clanDescriptionText, detailedClanInfo.description);
        SetText(leaderNameText, detailedClanInfo.leaderName);
        SetText(memberCountText, $"{detailedClanInfo.memberCount}/50");
        SetText(creationDateText, FormatDate(detailedClanInfo.creationDate));
        
        // Display member list if available
        if (memberListParent != null && detailedClanInfo.members != null)
        {
            DisplayMemberList();
        }
    }
    
    private void DisplayMemberList()
    {
        // Clear existing member items
        foreach (Transform child in memberListParent)
        {
            if (child != memberListParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Create member items
        foreach (var member in detailedClanInfo.members)
        {
            if (memberItemPrefab != null)
            {
                var memberItem = Instantiate(memberItemPrefab, memberListParent);
                var memberText = memberItem.GetComponentInChildren<TMP_Text>();
                
                if (memberText != null)
                {
                    string role = member.isLeader ? "Leader" : "Member";
                    memberText.text = $"{member.username} ({role})";
                }
            }
        }
    }
    
    private void SetText(TMP_Text textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text ?? "N/A";
        }
    }
    
    private string FormatDate(string dateString)
    {
        if (DateTime.TryParse(dateString, out DateTime date))
        {
            return date.ToString("MMM dd, yyyy");
        }
        return dateString ?? "Unknown";
    }
    
    #endregion
    
    #region User Actions Logic
    
    private void DetermineAvailableActions()
    {
        if (currentUser == null || detailedClanInfo == null)
        {
            HideAllActionButtons();
            return;
        }
        
        bool isCurrentClanMember = detailedClanInfo.members?.Any(m => m.id == currentUser.id) ?? false;
        bool isClanFull = detailedClanInfo.memberCount >= 50;
        
        Debug.Log($"User {currentUser.id} membership check: isCurrentClanMember={isCurrentClanMember}, isClanFull={isClanFull}");
        
        if (isCurrentClanMember)
        {
            // User is a member - show leave button
            SetButtonVisibility(joinButton, false);
            SetButtonVisibility(leaveButton, true);
        }
        else if (isClanFull)
        {
            // Clan is full and user is not a member - hide both buttons
            HideAllActionButtons();
        }
        else
        {
            // User is not a member and clan has space - show join button
            SetButtonVisibility(joinButton, true);
            SetButtonVisibility(leaveButton, false);
        }
    }
    
    #endregion
    
    #region Clan Actions
    
    private async Task JoinClanAsync()
    {
        if (currentClan == null) return;
        
        try
        {
            ShowStatus("Joining clan...", false);
            SetButtonInteractable(joinButton, false);
            
            var response = await ApiClient.PostAsync<object, ClanActionResponse>($"/clans/{currentClan.id}/join", null);
            
            if (response?.success == true)
            {
                ShowStatus("Successfully joined clan!", false);
                await RefreshClanInfoAsync();
            }
            else
            {
                ShowStatus(response?.message ?? "Failed to join clan", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                400 => "Cannot join - you may already be in a clan or this clan may be full",
                403 => "You are already in another clan",
                404 => "Clan not found",
                _ => "Failed to join clan"
            };
            
            ShowStatus(errorMessage, true);
            Debug.LogError($"Join clan error: {ex.StatusCode} - {ex.Message}");
            
            // Handle specific error cases
            if (ex.StatusCode == 403)
            {
                // User is in another clan - hide both buttons
                HideAllActionButtons();
            }
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to join clan", true);
            Debug.LogError($"Join clan error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(joinButton, true);
        }
    }
    
    private async Task LeaveClanAsync()
    {
        if (currentClan == null) return;
        
        try
        {
            ShowStatus("Leaving clan...", false);
            SetButtonInteractable(leaveButton, false);
            
            var response = await ApiClient.PostAsync<object, ClanLeaveResponse>($"/clans/{currentClan.id}/leave", null);
            
            if (response?.success == true)
            {
                string message = response.action == "disbanded" 
                    ? "Clan disbanded (you were the only member)" 
                    : "Successfully left clan!";
                
                ShowStatus(message, false);
                await RefreshClanInfoAsync();
            }
            else
            {
                ShowStatus(response?.message ?? "Failed to leave clan", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                400 => "Cannot leave - you may not be a member or may need to transfer leadership first",
                403 => "Permission denied",
                404 => "Clan not found",
                _ => "Failed to leave clan"
            };
            
            ShowStatus(errorMessage, true);
            Debug.LogError($"Leave clan error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to leave clan", true);
            Debug.LogError($"Leave clan error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(leaveButton, true);
        }
    }
    
    #endregion
    
    #region UI Utilities
    
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.white;
        }
    }
    
    private void SetLoadingState(bool loading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(loading);
        }
        
        // Disable interactive buttons during loading
        SetButtonInteractable(joinButton, !loading);
        SetButtonInteractable(leaveButton, !loading);
        SetButtonInteractable(refreshButton, !loading);
    }
    
    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
    
    private void ClearClanData()
    {
        currentClan = null;
        currentUser = null;
        detailedClanInfo = null;
        clanManager = null;
    }
    
    #endregion
}

#region Data Structures

[Serializable]
public class ClanDetailResponse
{
    public bool success;
    public string message;
    public ClanDetailData clan;
}

[Serializable]
public class ClanDetailData
{
    public int id;
    public string name;
    public string tag;
    public string description;
    public string leaderName;
    public int memberCount;
    public string creationDate;
    public ClanMember[] members;
}

[Serializable]
public class ClanMember
{
    public int id;
    public string username;
    public bool isLeader;
    public string joinedDate;
}

[Serializable]
public class ClanActionResponse
{
    public bool success;
    public string message;
    public ClanData clan;
}

[Serializable]
public class ClanLeaveResponse
{
    public bool success;
    public string message;
    public string action; // "left" or "disbanded"
    public ClanData clan;
}

#endregion