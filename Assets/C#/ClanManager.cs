using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Threading.Tasks;

// Main clan management controller that handles loading clans and displaying them in a scroll view
public class ClanManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform clanListParent; // The Content transform of the scroll view
    [SerializeField] private GameObject clanCardPrefab; // The clan card prefab to instantiate
    [SerializeField] private TMP_Text statusText; // Status text for loading/error messages
    [SerializeField] private Button refreshButton; // Button to refresh clan list
    [SerializeField] private Button createClanButton; // Button to open clan creation modal
    
    [Header("Clan Status Panels")]
    [SerializeField] private GameObject hasClanPanel; // Panel shown when user is in a clan
    [SerializeField] private GameObject noClanPanel; // Panel shown when user is not in a clan
    [SerializeField] private Button viewMyClanButton; // Button to view current user's clan details
    [SerializeField] private TMP_Text myClanNameText; // Text showing current clan name
    [SerializeField] private TMP_Text myClanMemberCountText; // Text showing current clan member count
    
    [Header("Modal Windows")]
    [SerializeField] private GameObject clanModalWindow; // Clan detail modal window GameObject
    [SerializeField] private GameObject clanCreateModalWindow; // Clan creation modal window GameObject
    
    private ClanDetailModal clanDetailModal; // Will be found automatically from modal window
    private ClanCreateModal clanCreateModal; // Will be found automatically from creation modal
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private ClanListResponse currentClans;
    private UserClanStatusResponse userClanStatus;
    
    private void Start()
    {
        SetupUI();
        _ = LoadInitialDataAsync();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (clanCreateModal != null)
        {
            clanCreateModal.OnClanCreated -= OnClanCreated;
            clanCreateModal.OnClanCreationCancelled -= OnClanCreationCancelled;
        }
    }
    
    private void SetupUI()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() => _ = LoadClansAsync());
        }
        
        if (createClanButton != null)
        {
            createClanButton.onClick.AddListener(ShowCreateClanModal);
        }
        
        if (viewMyClanButton != null)
        {
            viewMyClanButton.onClick.AddListener(() => _ = ShowMyClanDetailsAsync());
        }
        
        if (statusText != null)
        {
            statusText.text = "Loading clans...";
        }
        
        // Setup detail modal
        if (clanModalWindow != null)
        {
            clanModalWindow.SetActive(false);
            
            // Find ClanDetailModal component on the modal window or its children
            clanDetailModal = clanModalWindow.GetComponent<ClanDetailModal>();
            if (clanDetailModal == null)
            {
                clanDetailModal = clanModalWindow.GetComponentInChildren<ClanDetailModal>();
            }
            
            if (clanDetailModal == null)
            {
                LogDebug("Warning: ClanDetailModal component not found on modal window or its children");
            }
        }
        
        // Setup clan creation modal
        if (clanCreateModalWindow != null)
        {
            clanCreateModalWindow.SetActive(false);
            
            // Find ClanCreateModal component on the modal window or its children
            clanCreateModal = clanCreateModalWindow.GetComponent<ClanCreateModal>();
            if (clanCreateModal == null)
            {
                clanCreateModal = clanCreateModalWindow.GetComponentInChildren<ClanCreateModal>();
            }
            
            if (clanCreateModal == null)
            {
                LogDebug("Warning: ClanCreateModal component not found on creation modal window or its children");
            }
            else
            {
                // Subscribe to clan creation events
                clanCreateModal.OnClanCreated += OnClanCreated;
                clanCreateModal.OnClanCreationCancelled += OnClanCreationCancelled;
            }
        }
    }
    
    // Load initial data including user clan status and clan list
    private async Task LoadInitialDataAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Authentication required", true);
            SetPanelVisibility(false); // Hide both panels if not authenticated
            return;
        }
        
        ShowStatus("Loading...", false);
        LogDebug("Loading initial clan data");
        
        try
        {
            // Load user clan status first to determine which panel to show
            await LoadUserClanStatusAsync();
            
            // Then load clan list
            await LoadClansAsync();
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to load clan data", true);
            LogDebug($"Initial data load error: {ex.Message}");
            SetPanelVisibility(false); // Hide both panels on error
        }
    }
    
    // Load user's current clan membership status using existing clan routes
    private async Task LoadUserClanStatusAsync()
    {
        try
        {
            LogDebug("Checking user clan membership status using existing routes");
            
            await CheckClanMembershipUsingExistingRoutes();
        }
        catch (Exception ex)
        {
            LogDebug($"User clan status error: {ex.Message}");
            SetPanelVisibility(false); // Default to no clan on error
        }
    }
    
    // Check clan membership by examining all clans using existing routes
    // This is more efficient than creating a new endpoint
    private async Task CheckClanMembershipUsingExistingRoutes()
    {
        try
        {
            LogDebug("Checking clan membership using existing routes");
            
            // Get current user info using existing user profile endpoint
            var userResponse = await ApiClient.GetAsync<UserProfileResponse>("/user");
            if (userResponse?.user == null)
            {
                SetPanelVisibility(false);
                return;
            }
            
            var currentUser = userResponse.user;
            
            // Get all clans to check membership
            var clansResponse = await ApiClient.GetAsync<ClanListResponse>("/clans");
            if (clansResponse?.clans != null)
            {
                bool isInAnyClan = false;
                ClanMembershipInfo membershipInfo = null;
                
                // Check each clan for detailed info that includes members
                foreach (var clan in clansResponse.clans)
                {
                    try
                    {
                        var clanDetail = await ApiClient.GetAsync<ClanDetailResponse>($"/clans/{clan.id}");
                        if (clanDetail?.clan?.members != null)
                        {
                            var memberInfo = clanDetail.clan.members.FirstOrDefault(m => m.id == currentUser.id);
                            if (memberInfo != null)
                            {
                                isInAnyClan = true;
                                membershipInfo = new ClanMembershipInfo
                                {
                                    clanId = clan.id,
                                    clanName = clan.name,
                                    clanTag = clan.tag,
                                    rank = memberInfo.isLeader ? "Leader" : "Member", // We don't have rank info in the current structure
                                    joinDate = memberInfo.joinedDate
                                };
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Skip this clan if we can't check its details
                        continue;
                    }
                }
                
                userClanStatus = new UserClanStatusResponse 
                { 
                    success = true, 
                    isInClan = isInAnyClan,
                    clanInfo = membershipInfo
                };
                
                SetPanelVisibility(isInAnyClan);
                LogDebug($"Clan membership check complete: isInClan={isInAnyClan}");
                
                if (isInAnyClan && membershipInfo != null)
                {
                    LogDebug($"User is member of clan: {membershipInfo.clanName} ({membershipInfo.clanTag}) as {membershipInfo.rank}");
                }
            }
            else
            {
                SetPanelVisibility(false);
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Clan membership check error: {ex.Message}");
            SetPanelVisibility(false);
        }
    }
    
    // Set panel visibility based on clan membership status
    private void SetPanelVisibility(bool hasClan)
    {
        if (hasClanPanel != null)
        {
            hasClanPanel.SetActive(hasClan);
        }
        
        if (noClanPanel != null)
        {
            noClanPanel.SetActive(!hasClan);
        }
        
        // Enable/disable the view my clan button based on clan membership
        if (viewMyClanButton != null)
        {
            viewMyClanButton.interactable = hasClan && userClanStatus?.isInClan == true;
        }
        
        // Update clan info texts
        UpdateMyClanInfoTexts();
        
        LogDebug($"Panel visibility set: hasClan={hasClan}");
    }
    
    // Update the clan name and member count texts for the current user's clan
    private async void UpdateMyClanInfoTexts()
    {
        if (userClanStatus?.isInClan == true && userClanStatus.clanInfo != null)
        {
            var clanInfo = userClanStatus.clanInfo;
            
            // Set clan name
            if (myClanNameText != null)
            {
                myClanNameText.text = $"{clanInfo.clanName}";
            }
            
            // Get detailed clan info to show current member count
            try
            {
                var clanDetail = await ApiClient.GetAsync<ClanDetailResponse>($"/clans/{clanInfo.clanId}");
                if (clanDetail?.clan != null && myClanMemberCountText != null)
                {
                    myClanMemberCountText.text = $"{clanDetail.clan.memberCount}/50";
                }
            }
            catch
            {
                // Fallback if we can't get detailed info
                if (myClanMemberCountText != null)
                {
                    myClanMemberCountText.text = "?/50";
                }
            }
        }
        else
        {
            // Clear texts when user has no clan
            if (myClanNameText != null)
            {
                myClanNameText.text = "No Clan";
            }
            
            if (myClanMemberCountText != null)
            {
                myClanMemberCountText.text = "";
            }
        }
    }
    
    // Load clans from the server and populate the UI
    public async Task LoadClansAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Authentication required", true);
            return;
        }
        
        ShowStatus("Loading clans...", false);
        LogDebug("Fetching clans from server");
        
        try
        {
            // Clear existing clan cards
            ClearClanList();
            
            var response = await ApiClient.GetAsync<ClanListResponse>("/clans");
            
            if (response?.clans != null)
            {
                currentClans = response;
                PopulateClanList();
                ShowStatus($"Loaded {response.clans.Length} clans", false);
                LogDebug($"Clans loaded successfully: {response.clans.Length} clans");
            }
            else
            {
                ShowStatus("Failed to load clans", true);
                LogDebug("Clans response was null");
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                401 => "Authentication failed. Please login.",
                404 => "Clans endpoint not found.",
                500 => "Server error. Please try again later.",
                _ => $"Error: {ex.Message}"
            };
            
            ShowStatus(errorMessage, true);
            LogDebug($"Clans API error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Network error. Please check connection.", true);
            LogDebug($"Clans error: {ex.Message}");
        }
    }
    
    private void ClearClanList()
    {
        if (clanListParent == null) return;
        
        // Destroy all existing clan cards
        for (int i = clanListParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(clanListParent.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(clanListParent.GetChild(i).gameObject);
            }
        }
    }
    
    private void PopulateClanList()
    {
        if (clanListParent == null || clanCardPrefab == null || currentClans?.clans == null) return;
        
        foreach (var clan in currentClans.clans)
        {
            GameObject clanCard = Instantiate(clanCardPrefab, clanListParent);
            
            // Setup the clan card
            var clanCardScript = clanCard.GetComponent<ClanCard>();
            if (clanCardScript != null)
            {
                clanCardScript.SetupClan(clan, this);
            }
            else
            {
                // Fallback: try to find components manually
                SetupClanCardFallback(clanCard, clan);
            }
        }
        
        LogDebug($"Populated {currentClans.clans.Length} clan cards in scroll view");
    }
    
    private void SetupClanCardFallback(GameObject clanCard, ClanData clan)
    {
        // Try to find common component names and set them
        var nameText = clanCard.transform.Find("ClanName")?.GetComponent<TMP_Text>();
        var memberCountText = clanCard.transform.Find("MemberCount")?.GetComponent<TMP_Text>();
        
        if (nameText != null) nameText.text = clan.name;
        if (memberCountText != null) memberCountText.text = $"Members: {clan.memberCount}";
    }
    
    // Show clan details in modal window
    public void ShowClanDetails(ClanData clan)
    {
        if (clanDetailModal != null && clanModalWindow != null)
        {
            clanDetailModal.ShowModal(clan, this); // Pass the manager reference
            clanModalWindow.SetActive(true);
        }
        else
        {
            LogDebug($"Modal components not assigned. Clan: {clan.name}");
        }
    }
    
    // Show the clan creation modal
    public void ShowCreateClanModal()
    {
        Debug.Log("Create Clan button clicked");
        if (clanCreateModal != null && clanCreateModalWindow != null)
        {
            clanCreateModal.ShowModal(this);
            clanCreateModalWindow.SetActive(true);
            LogDebug("Showing clan creation modal");
        }
        else
        {
            LogDebug("Cannot show clan creation modal - modal components not assigned");
        }
    }
    
    // Show the current user's clan details in the detail modal
    public async Task ShowMyClanDetailsAsync()
    {
        LogDebug("View My Clan button clicked");
        
        if (userClanStatus?.isInClan != true || userClanStatus.clanInfo == null)
        {
            ShowStatus("You are not currently in a clan", true);
            LogDebug("Cannot show clan details - user is not in a clan");
            return;
        }
        
        try
        {
            ShowStatus("Loading your clan details...", false);
            
            // Get the user's clan information
            var clanInfo = userClanStatus.clanInfo;
            
            // Create a ClanData object from the membership info
            var clanData = new ClanData
            {
                id = clanInfo.clanId,
                name = clanInfo.clanName,
                tag = clanInfo.clanTag,
                description = "", // Will be loaded in detail modal
                leaderName = "", // Will be loaded in detail modal
                memberCount = 0, // Will be loaded in detail modal
                creationDate = clanInfo.joinDate
            };
            
            // Show the clan details modal
            ShowClanDetails(clanData);
            
            LogDebug($"Showing details for user's clan: {clanInfo.clanName} ({clanInfo.clanTag})");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to load your clan details", true);
            LogDebug($"Error showing user's clan details: {ex.Message}");
        }
    }
    
    // Hide clan details modal window
    public void HideClanDetails()
    {
        if (clanModalWindow != null)
        {
            clanModalWindow.SetActive(false);
        }
    }
    
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
        
        LogDebug($"Status: {message}");
    }
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ClanManager] {message}");
        }
    }
    
    #region Clan Creation Events
    
    /// <summary>
    /// Called when a new clan is successfully created
    /// </summary>
    /// <param name="newClan">The newly created clan data</param>
    private void OnClanCreated(ClanData newClan)
    {
        LogDebug($"New clan created: {newClan.name} ({newClan.tag})");
        
        // User now has a clan, so show the hasClan panel immediately
        SetPanelVisibility(true);
        
        // Refresh all data to update the clan list and confirm status
        _ = LoadInitialDataAsync();
    }
    
    /// <summary>
    /// Called when clan creation is cancelled
    /// </summary>
    private void OnClanCreationCancelled()
    {
        LogDebug("Clan creation cancelled");
    }
    
    #endregion
    
    // Context menu methods for testing
    [ContextMenu("Refresh Clans")]
    public void RefreshClans()
    {
        _ = LoadClansAsync();
    }
    
    [ContextMenu("Refresh All Data")]
    public void RefreshAllData()
    {
        _ = LoadInitialDataAsync();
    }
    
    // Public method to refresh clan membership status (call after joining/leaving clans)
    public async Task RefreshMembershipStatusAsync()
    {
        await LoadUserClanStatusAsync();
        // Update texts after status refresh
        UpdateMyClanInfoTexts();
    }
    
    // Called when user joins a clan to immediately update panel visibility
    public void OnUserJoinedClan()
    {
        LogDebug("User joined a clan - updating panel visibility");
        
        // User now has a clan, so show the hasClan panel immediately
        SetPanelVisibility(true);
        
        // Refresh membership status to confirm and get updated info
        _ = RefreshMembershipStatusAsync();
        
        // Also refresh clan list to show updated member counts
        _ = LoadClansAsync();
    }
    
    // Called when user leaves a clan to immediately update panel visibility
    public void OnUserLeftClan()
    {
        LogDebug("User left a clan - updating panel visibility");
        
        // User no longer has a clan, so show the noClan panel immediately
        SetPanelVisibility(false);
        
        // Refresh membership status to confirm and get updated info
        _ = RefreshMembershipStatusAsync();
        
        // Also refresh clan list to show updated member counts
        _ = LoadClansAsync();
    }
    
    [ContextMenu("Clear Display")]
    public void ClearDisplay()
    {
        ClearClanList();
        if (statusText != null)
        {
            statusText.text = "Clan list cleared";
        }
    }
    
    [ContextMenu("Show My Clan")]
    public void ShowMyClanDetailsContextMenu()
    {
        _ = ShowMyClanDetailsAsync();
    }
    
    [ContextMenu("Update Clan Info Texts")]
    public void UpdateClanInfoTextsContextMenu()
    {
        UpdateMyClanInfoTexts();
    }
}

// Data structures for clan API responses
[Serializable]
public class ClanListResponse
{
    public bool success;
    public string message;
    public int totalClans;
    public ClanData[] clans;
}

[Serializable]
public class ClanData
{
    public int id;
    public string name;
    public string tag;
    public string description;
    public string leaderName;
    public int memberCount;
    public string creationDate;
}

[Serializable]
public class UserClanStatusResponse
{
    public bool success;
    public string message;
    public bool isInClan;
    public ClanMembershipInfo clanInfo;
}

[Serializable]
public class ClanMembershipInfo
{
    public int clanId;
    public string clanName;
    public string clanTag;
    public string rank;
    public string joinDate;
}