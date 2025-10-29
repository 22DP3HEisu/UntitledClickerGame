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
    [SerializeField] private Button createClanButton; // Button to open clan creation modal
    
    [Header("Modal Windows")]
    [SerializeField] private GameObject clanModalWindow; // Clan detail modal window GameObject
    [SerializeField] private GameObject clanCreateModalWindow; // Clan creation modal window GameObject
    
    private ClanDetailModal clanDetailModal; // Will be found automatically from modal window
    private ClanCreateModal clanCreateModal; // Will be found automatically from creation modal
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private ClanListResponse currentClans;
    
    private void Start()
    {
        SetupUI();
        _ = LoadClansAsync();
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
        var memberCountText = clanCard.transform.Find("MemberCount")?.GetComponent<TMP_Text>();
        
        if (nameText != null) nameText.text = clan.name;
        if (memberCountText != null) memberCountText.text = $"Members: {clan.memberCount}";
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
    /// Show the clan creation modal
    /// </summary>
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
    
    #region Clan Creation Events
    
    /// <summary>
    /// Called when a new clan is successfully created
    /// </summary>
    /// <param name="newClan">The newly created clan data</param>
    private void OnClanCreated(ClanData newClan)
    {
        LogDebug($"New clan created: {newClan.name} ({newClan.tag})");
        
        // Refresh the clan list to show the new clan
        _ = LoadClansAsync();
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