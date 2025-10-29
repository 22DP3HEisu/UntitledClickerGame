using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Modal window for displaying detailed clan information and join functionality
/// </summary>
public class ClanDetailModal : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanTagText;
    [SerializeField] private TMP_Text clanDescriptionText;
    [SerializeField] private TMP_Text leaderNameText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private TMP_Text creationDateText;
    [SerializeField] private TMP_Text statusText;
    
    [Header("Actions")]
    [SerializeField] private Button joinButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;
    
    [Header("Visual Elements")]
    [SerializeField] private Image clanBanner; // Optional clan banner/icon
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Member List (Optional)")]
    [SerializeField] private Transform memberListParent;
    [SerializeField] private GameObject memberItemPrefab;
    
    private ClanData currentClan;
    private ClanManager clanManager;
    
    private void Awake()
    {
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(JoinClan);
        }
        
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(LeaveClan);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseModal);
        }
        
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => _ = LoadDetailedClanInfo());
        }
    }
    
    /// <summary>
    /// Show clan details in the modal
    /// </summary>
    public void ShowClanDetails(ClanData clan)
    {
        currentClan = clan;
        
        // Find clan manager in scene if not set
        if (clanManager == null)
        {
            clanManager = FindObjectOfType<ClanManager>();
        }
        
        UpdateDisplay();
        
        // Check user membership status and update button visibility
        _ = CheckUserMembershipAndUpdateButtons();
        
        // Load detailed information if possible
        _ = LoadDetailedClanInfo();
    }
    
    private void UpdateDisplay()
    {
        if (currentClan == null) return;
        
        // Update basic clan information
        if (clanNameText != null) clanNameText.text = currentClan.name;
        if (clanTagText != null) clanTagText.text = $"[{currentClan.tag}]";
        if (clanDescriptionText != null) 
        {
            clanDescriptionText.text = !string.IsNullOrEmpty(currentClan.description) 
                ? currentClan.description 
                : "No description available";
        }
        if (leaderNameText != null) leaderNameText.text = $"Leader: {currentClan.leaderName}";
        if (memberCountText != null) memberCountText.text = $"Members:\n{currentClan.memberCount}";
        
        // Format creation date
        if (creationDateText != null)
        {
            try
            {
                if (System.DateTime.TryParse(currentClan.creationDate, out System.DateTime date))
                {
                    creationDateText.text = $"Created: {date:MMMM dd, yyyy}";
                }
                else
                {
                    creationDateText.text = "Created: Unknown";
                }
            }
            catch
            {
                creationDateText.text = "Created: Unknown";
            }
        }
        
        ShowStatus("Clan information loaded", false);
    }
    
    private async Task LoadDetailedClanInfo()
    {
        if (currentClan == null) return;
        
        ShowLoading(true);
        ShowStatus("Loading detailed clan information...", false);
        
        try
        {
            // This would require an endpoint like /clans/{id} for detailed info
            // For now, we'll use the basic information we already have
            
            // Simulate loading delay
            await Task.Delay(500);
            
            // If you have a detailed clan endpoint, use it like this:
            /*
            var detailedClan = await ApiClient.GetAsync<DetailedClanResponse>($"/clans/{currentClan.id}");
            if (detailedClan?.clan != null)
            {
                // Update with detailed information
                LoadMemberList(detailedClan.clan.members);
            }
            */
            
            ShowStatus("Clan details loaded", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to load details: {ex.Message}", true);
            Debug.LogError($"Error loading clan details: {ex.Message}");
        }
        finally
        {
            ShowLoading(false);
        }
    }
    
    /// <summary>
    /// Check user's clan membership status and update button visibility accordingly
    /// This uses a simpler approach by trying to join/leave and handling the API response
    /// </summary>
    private async Task CheckUserMembershipAndUpdateButtons()
    {
        if (currentClan == null) return;
        
        try
        {
            ShowStatus("Checking membership status...", false);
            
            // Get user profile first to get current user ID
            var userProfile = await ApiClient.GetAsync<UserProfileResponse>("/user");
            if (userProfile?.user?.id == null)
            {
                Debug.LogWarning("Could not get user profile, showing default buttons");
                ShowDefaultButtons();
                ShowStatus("Ready", false);
                return;
            }
            
            int currentUserId = userProfile.user.id;
            Debug.Log($"Current user ID: {currentUserId}");
            
            // Get detailed clan information to check membership
            var detailedClan = await ApiClient.GetAsync<DetailedClanResponse>($"/clans/{currentClan.id}");
            
            if (detailedClan?.clan?.members != null)
            {
                // Check if current user is a member of this clan
                bool isCurrentClanMember = detailedClan.clan.members.Any(member => member.userId == currentUserId);
                
                if (isCurrentClanMember)
                {
                    // User is a member of this clan - show leave button
                    HideJoinButton();
                    ShowLeaveButton();
                    Debug.Log("User is a member of this clan - showing leave button");
                }
                else
                {
                    // User is not a member of this clan
                    // Check if clan is full
                    if (detailedClan.clan.memberCount >= 50)
                    {
                        // Clan is full - hide both buttons
                        HideJoinButton();
                        HideLeaveButton();
                        Debug.Log("Clan is full - hiding both buttons");
                    }
                    else
                    {
                        // Clan has space - show join button (API will handle if user is in another clan)
                        ShowJoinButton();
                        HideLeaveButton();
                        Debug.Log("User is not a member and clan has space - showing join button");
                    }
                }
                ShowStatus("Ready", false);
            }
            else
            {
                // Fallback: show default buttons
                ShowDefaultButtons();
                ShowStatus("Ready", false);
            }
        }
        catch (ApiException ex)
        {
            Debug.LogWarning($"Could not check membership status: {ex.StatusCode} - {ex.Message}");
            // Show default buttons anyway - let API calls handle the validation
            ShowDefaultButtons();
            ShowStatus("Ready", false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking membership status: {ex.Message}");
            ShowDefaultButtons();
            ShowStatus("Ready", false);
        }
    }
    
    /// <summary>
    /// Show default button configuration - both buttons visible, let API responses determine behavior
    /// </summary>
    private void ShowDefaultButtons()
    {
        // Show join button if clan isn't full
        if (currentClan.memberCount < 50)
        {
            ShowJoinButton();
        }
        else
        {
            HideJoinButton();
        }
        
        // Always show leave button - API will return appropriate error if user isn't a member
        ShowLeaveButton();
    }
    
    private void ShowJoinButton()
    {
        if (joinButton != null)
        {
            joinButton.gameObject.SetActive(true);
            joinButton.interactable = true;
        }
    }
    
    private void HideJoinButton()
    {
        if (joinButton != null)
        {
            joinButton.gameObject.SetActive(false);
        }
    }
    
    private void ShowLeaveButton()
    {
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
            leaveButton.interactable = true;
        }
    }
    
    private void HideLeaveButton()
    {
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(false);
        }
    }
    
    private void HideAllActionButtons()
    {
        HideJoinButton();
        HideLeaveButton();
    }
    
    private async void JoinClan()
    {
        Debug.Log("JoinClan button clicked");
        if (currentClan == null) return;
        
        ShowStatus("Joining clan...", false);
        joinButton.interactable = false;
        
        try
        {
            Debug.Log($"Attempting to join clan: {currentClan.name} (ID: {currentClan.id})");
            
            var response = await ApiClient.PostAsync<object, JoinClanResponse>($"/clans/{currentClan.id}/join", null);
            if (response != null && response.success)
            {
                ShowStatus("Successfully joined clan!", false);
                
                // Update button visibility - user is now a member
                HideJoinButton();
                ShowLeaveButton();
                
                // Refresh clan list if clan manager is available
                if (clanManager != null)
                {
                    _ = clanManager.LoadClansAsync();
                }
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
                _ => $"Failed to join clan: {ex.Message}"
            };
            ShowStatus(errorMessage, true);
            Debug.LogError($"Error joining clan: {ex.StatusCode} - {ex.Message}");
            
            // If user is already in another clan (403), hide both buttons
            if (ex.StatusCode == 403)
            {
                HideJoinButton();
                HideLeaveButton();
            }
            // If already a member of this clan or has other issues (400), adjust buttons accordingly
            else if (ex.StatusCode == 400)
            {
                HideJoinButton();
                // If they're already a member of this clan, show leave button
                if (ex.Message.Contains("already"))
                {
                    ShowLeaveButton();
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to join clan: {ex.Message}", true);
            Debug.LogError($"Error joining clan: {ex.Message}");
        }
        finally
        {
            joinButton.interactable = true;
        }
    }
    
    private async void LeaveClan()
    {
        if (currentClan == null) return;
        
        ShowStatus("Leaving clan...", false);
        leaveButton.interactable = false;
        
        try
        {
            Debug.Log($"Attempting to leave clan: {currentClan.name} (ID: {currentClan.id})");
            
            var response = await ApiClient.PostAsync<object, LeaveClanResponse>($"/clans/{currentClan.id}/leave", null);
            if (response != null && response.success)
            {
                string message = response.action == "disbanded" 
                    ? "Clan disbanded as you were the only member" 
                    : "Successfully left clan!";
                ShowStatus(message, false);
                
                // Update button visibility - user is no longer a member
                HideLeaveButton();
                if (currentClan.memberCount < 50)
                {
                    ShowJoinButton();
                }
                
                // Refresh clan list if clan manager is available
                if (clanManager != null)
                {
                    _ = clanManager.LoadClansAsync();
                }
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
                _ => $"Failed to leave clan: {ex.Message}"
            };
            ShowStatus(errorMessage, true);
            Debug.LogError($"Error leaving clan: {ex.StatusCode} - {ex.Message}");
            
            // If user is not a member, hide leave button
            if (ex.StatusCode == 400)
            {
                HideLeaveButton();
                if (currentClan.memberCount < 50)
                {
                    ShowJoinButton();
                }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to leave clan: {ex.Message}", true);
            Debug.LogError($"Error leaving clan: {ex.Message}");
        }
        finally
        {
            leaveButton.interactable = true;
        }
    }
    
    public void CloseModal()
    {
        if (clanManager != null)
        {
            clanManager.HideClanDetails();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }
    
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
        
        Debug.Log($"[ClanDetailModal] {message}");
    }
    
    // Optional: Load member list if you have detailed clan info
    private void LoadMemberList(ClanMember[] members)
    {
        if (memberListParent == null || memberItemPrefab == null || members == null) return;
        
        // Clear existing member items
        for (int i = memberListParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(memberListParent.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(memberListParent.GetChild(i).gameObject);
            }
        }
        
        // Create member items
        foreach (var member in members)
        {
            GameObject memberItem = Instantiate(memberItemPrefab, memberListParent);
            
            // Setup member item (you'd need to create a ClanMemberItem component)
            var memberText = memberItem.GetComponentInChildren<TMP_Text>();
            if (memberText != null)
            {
                memberText.text = $"{member.username} ({member.rank})";
            }
        }
    }
}

// Data structures for detailed clan information
[Serializable]
public class DetailedClanResponse
{
    public bool success;
    public string message;
    public DetailedClanData clan;
}

[Serializable]
public class DetailedClanData : ClanData
{
    public ClanMember[] members;
}

[Serializable]
public class ClanMember
{
    public int userId;
    public string username;
    public string rank;
    public string joinDate;
}

[Serializable]
public class JoinClanResponse
{
    public bool success;
    public string message;
    public ClanData clan;
}

[Serializable]
public class LeaveClanResponse
{
    public bool success;
    public string message;
    public string action; // "left" or "disbanded"
    public ClanData clan;
}