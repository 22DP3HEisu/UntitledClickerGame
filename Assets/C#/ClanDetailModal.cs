using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;
    
    [Header("Visual Elements")]
    [SerializeField] private Image clanBanner; // Optional clan banner/icon
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Member List (Optional)")]
    [SerializeField] private Transform memberListParent;
    [SerializeField] private GameObject memberItemPrefab;
    [SerializeField] private ScrollRect memberScrollView;
    
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
        if (memberCountText != null) memberCountText.text = $"Members: {currentClan.memberCount}";
        
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
        
        // Update join button state
        UpdateJoinButtonState();
        
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
    
    private void UpdateJoinButtonState()
    {
        if (joinButton == null) return;
        
        // Check if user is already in a clan or if this clan is full
        // For now, enable the button - you can add logic here
        joinButton.interactable = true;
        
        var buttonText = joinButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "Join Clan";
        }
        
        // Example logic for button states:
        /*
        if (UserManager.IsInClan())
        {
            joinButton.interactable = false;
            buttonText.text = "Already in Clan";
        }
        else if (currentClan.memberCount >= 50) // Assuming max 50 members
        {
            joinButton.interactable = false;
            buttonText.text = "Clan Full";
        }
        */
    }
    
    private async void JoinClan()
    {
        if (currentClan == null) return;
        
        ShowStatus("Joining clan...", false);
        joinButton.interactable = false;
        
        try
        {
            Debug.Log($"Attempting to join clan: {currentClan.name} (ID: {currentClan.id})");
            
            // This would require a join clan endpoint
            // For now, show a placeholder message
            await Task.Delay(1000); // Simulate network delay
            
            // Example API call:
            /*
            var response = await ApiClient.PostAsync<object, JoinClanResponse>($"/clans/{currentClan.id}/join", null);
            if (response != null && response.success)
            {
                ShowStatus("Successfully joined clan!", false);
                CloseModal();
                // Refresh clan list or user profile
            }
            */
            
            ShowStatus("Join functionality not implemented yet", true);
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