using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// Main clan management controller that handles loading clans and displaying them in a scroll view
/// </summary>
public class ClanManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform clanListParent; // The Content transform of the scroll view
    [SerializeField] private GameObject clanCardPrefab; // The clan card prefab to instantiate
    [SerializeField] private TMP_Text statusText; // Status text for loading/error messages
    [SerializeField] private Button refreshButton; // Button to refresh clan list
    
    [Header("Modal Window")]
    [SerializeField] private GameObject clanModalWindow; // Modal window GameObject
    [SerializeField] private ClanDetailModal clanDetailModal; // Modal window controller
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private ClanListResponse currentClans;
    
    private void Start()
    {
        SetupUI();
        _ = LoadClansAsync();
    }
    
    private void SetupUI()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() => _ = LoadClansAsync());
        }
        
        if (statusText != null)
        {
            statusText.text = "Loading clans...";
        }
        
        // Ensure modal starts hidden
        if (clanModalWindow != null)
        {
            clanModalWindow.SetActive(false);
        }
    }
    
    /// <summary>
    /// Load clans from the server and populate the UI
    /// </summary>
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
        var tagText = clanCard.transform.Find("ClanTag")?.GetComponent<TMP_Text>();
        var memberCountText = clanCard.transform.Find("MemberCount")?.GetComponent<TMP_Text>();
        var joinButton = clanCard.transform.Find("JoinButton")?.GetComponent<Button>();
        
        if (nameText != null) nameText.text = clan.name;
        if (tagText != null) tagText.text = $"[{clan.tag}]";
        if (memberCountText != null) memberCountText.text = $"Members: {clan.memberCount}";
        
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(() => ShowClanDetails(clan));
        }
    }
    
    /// <summary>
    /// Show clan details in modal window
    /// </summary>
public void ShowClanDetails(ClanData clan)
    {
        if (clanDetailModal != null && clanModalWindow != null)
        {
            clanDetailModal.ShowClanDetails(clan);
            clanModalWindow.SetActive(true);
        }
        else
        {
            LogDebug($"Modal components not assigned. Clan: {clan.name}");
        }
    }
    
    /// <summary>
    /// Hide clan details modal window
    /// </summary>
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
    
    // Context menu methods for testing
    [ContextMenu("Refresh Clans")]
    public void RefreshClans()
    {
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