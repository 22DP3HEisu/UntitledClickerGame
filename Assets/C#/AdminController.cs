using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// Admin controller for managing administrative functions and displaying server statistics
/// </summary>
public class AdminController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text userStatsText;
    [SerializeField] private TMP_Text clanStatsText;
    [SerializeField] private TMP_Text statusText;
    
    [Header("User List")]
    [SerializeField] private Transform userListParent; // The Content transform of the scroll view
    [SerializeField] private GameObject userPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private AdminStatsResponse currentStats;
    private AdminUsersResponse currentUsers;
    
    private void Start()
    {
        SetupUI();

        // Always reload stats when admin page is loaded
        _ = LoadAdminStatsAsync();
        _ = LoadUsersListAsync();
    }
    
    private void SetupUI()
    {
        
        // Initialize display
        if (userStatsText != null)
        {
            userStatsText.text = "Loading user statistics...";
        }
        
        if (clanStatsText != null)
        {
            clanStatsText.text = "Loading clan statistics...";
        }
    }
    
    /// <summary>
    /// Load admin statistics from server
    /// </summary>
    public async Task LoadAdminStatsAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Authentication required", true);
            return;
        }
        
        ShowStatus("Loading admin statistics...", false);
        LogDebug("Fetching admin stats from server");
        
        try
        {
            var response = await ApiClient.GetAsync<AdminStatsResponse>("/admin/stats");
            
            if (response != null)
            {
                currentStats = response;
                DisplayStats();
                ShowStatus("Statistics loaded successfully", false);
                LogDebug("Admin stats loaded successfully");
            }
            else
            {
                ShowStatus("Failed to load statistics", true);
                LogDebug("Admin stats response was null");
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                401 => "Authentication failed. Please login as admin.",
                403 => "Admin privileges required.",
                404 => "Admin endpoint not found.",
                _ => $"Server error: {ex.Message}"
            };
            
            ShowStatus(errorMessage, true);
            LogDebug($"Admin stats API error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Network error. Please check connection.", true);
            LogDebug($"Admin stats error: {ex.Message}");
        }
        finally
        {
        }
    }
    
    private void DisplayStats()
    {
        if (currentStats == null) return;
        
        DisplayUserStats();
        DisplayClanStats();
    }
    
    private void DisplayUserStats()
    {
        if (userStatsText == null || currentStats?.accounts == null) return;
        
        var userText = $"Total Users: {currentStats.accounts.totalUsers:N0}";
        
        userStatsText.text = userText;
    }
    
    private void DisplayClanStats()
    {
        if (clanStatsText == null || currentStats?.clans == null) return;
        
        var clanText = $"Total Clans: {currentStats.clans.totalClans:N0}";
        
        clanStatsText.text = clanText;
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
            Debug.Log($"[AdminController] {message}");
        }
    }
    
    // Context menu methods for testing
    /// <summary>
    /// Load users list from server and populate the scroll view
    /// </summary>
    public async Task LoadUsersListAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Authentication required", true);
            return;
        }
        
        ShowStatus("Loading users list...", false);
        LogDebug("Fetching users list from server");
        
        try
        {
            // Clear existing user items
            ClearUserList();
            
            var response = await ApiClient.GetAsync<AdminUsersResponse>("/admin/users?limit=100");
            
            if (response?.users != null)
            {
                currentUsers = response;
                PopulateUserList();
                ShowStatus($"Loaded {response.users.Length} users", false);
                LogDebug($"Users list loaded successfully: {response.users.Length} users");
            }
            else
            {
                ShowStatus("Failed to load users list", true);
                LogDebug("Users list response was null");
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                401 => "Authentication failed. Please login as admin.",
                403 => "Admin privileges required.",
                404 => "Users endpoint not found.",
                _ => $"Server error: {ex.Message}"
            };
            
            ShowStatus(errorMessage, true);
            LogDebug($"Users list API error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Network error. Please check connection.", true);
            LogDebug($"Users list error: {ex.Message}");
        }
        finally
        {

        }
    }
    
    private void ClearUserList()
    {
        if (userListParent == null) return;
        
        // Destroy all existing user items
        for (int i = userListParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(userListParent.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(userListParent.GetChild(i).gameObject);
            }
        }
    }
    
    private void PopulateUserList()
    {
        if (userListParent == null || userPrefab == null || currentUsers?.users == null) return;
        
        foreach (var user in currentUsers.users)
        {
            GameObject userItem = Instantiate(userPrefab, userListParent);
            
            // Try to find and populate user item components
            // Assuming your user prefab has these components
            var userItemScript = userItem.GetComponent<AdminUserItem>();
            if (userItemScript != null)
            {
                userItemScript.SetupUser(user);
            }
            else
            {
                // Fallback: try to find text components by name
                SetupUserItemFallback(userItem, user);
            }
        }
        
        LogDebug($"Populated {currentUsers.users.Length} user items in scroll view");
    }
    
    private void SetupUserItemFallback(GameObject userItem, AdminUser user)
    {
        // Try to find common text component names and set them
        var usernameText = userItem.transform.Find("Name")?.GetComponent<TMP_Text>();
        var roleText = userItem.transform.Find("Role")?.GetComponent<TMP_Text>();
        
        if (usernameText != null) usernameText.text = user.username;
        if (roleText != null) roleText.text = user.role;
    }

    [ContextMenu("Refresh Stats")]
    public void RefreshStats()
    {
        _ = LoadAdminStatsAsync();
    }
    
    [ContextMenu("Load Users")]
    public void LoadUsers()
    {
        _ = LoadUsersListAsync();
    }
    
    [ContextMenu("Clear Display")]
    public void ClearDisplay()
    {
        if (userStatsText != null)
        {
            userStatsText.text = "User statistics cleared";
        }
        
        if (clanStatsText != null)
        {
            clanStatsText.text = "Clan statistics cleared";
        }
    }
}

// Data structures for admin API responses
[Serializable]
public class AdminStatsResponse
{
    public string message;
    public AccountStats accounts;
    public ClanStats clans;
    public TopClan[] topClans;
}

[Serializable]
public class AccountStats
{
    public int totalUsers;
    public int totalAdmins;
    public int activeLast24h;
}

[Serializable]
public class ClanStats
{
    public int totalClans;
    public int totalClanMemberships;
}

[Serializable]
public class TopClan
{
    public int id;
    public string name;
    public string tag;
    public int memberCount;
}

[Serializable]
public class AdminUsersResponse
{
    public string message;
    public AdminUser[] users;
    public UsersPagination pagination;
}

[Serializable]
public class AdminUser
{
    public int id;
    public string username;
    public string email;
    public string role;
    public int carrots;
    public int horseShoes;
    public int goldenCarrots;
    public string createdAt;
    public string updatedAt;
    public bool isBanned;
}

[Serializable]
public class UsersPagination
{
    public int currentPage;
    public int totalPages;
    public int totalUsers;
    public int usersPerPage;
}