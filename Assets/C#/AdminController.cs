using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AdminController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text userStatsText;
    [SerializeField] private TMP_Text clanStatsText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button backButton;

    [Header("User List")]
    [SerializeField] private Transform userListParent;
    [SerializeField] private GameObject userPrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Confirm UI")]
    [SerializeField] private ConfirmPanel confirmPanel;

    [Header("Edit User profile")]
    [SerializeField] private GameObject editUserPanelRoot;

    [Header("API Endpoint Formats")]
    [SerializeField] private string banEndpointFormat = "/admin/user/{0}/ban";
    [SerializeField] private string unbanEndpointFormat = "/admin/user/{0}/unban";
    [SerializeField] private string grantAdminEndpointFormat = "/admin/user/{0}/promote";
    [SerializeField] private string revokeAdminEndpointFormat = "/admin/user/{0}/demote";
    [SerializeField] private string deleteEndpointFormat = "/admin/user/{0}";
    [SerializeField] private string updateUserEndpointFormat = "/admin/user/{0}";

    private AdminStatsResponse currentStats;
    private AdminUsersResponse currentUsers;

    private void Start()
    {
        SetupUI();
        NormalizeEndpointFormats();
        _ = LoadAdminStatsAsync();
        _ = LoadUsersListAsync();
    }

    private void SetupUI()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (userStatsText != null)
            userStatsText.text = "Loading user statistics...";

        if (clanStatsText != null)
            clanStatsText.text = "Loading clan statistics...";
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("game");
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs) Debug.Log($"[AdminController] {message}");
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

    // Ensure commonly-mistyped/old endpoint formats are normalized at runtime.
    private void NormalizeEndpointFormats()
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(grantAdminEndpointFormat) &&
            (grantAdminEndpointFormat.Contains("grant-admin") || grantAdminEndpointFormat.Contains("grant_admin") || grantAdminEndpointFormat.Contains("grantAdmin")))
        {
            grantAdminEndpointFormat = "/admin/user/{0}/promote";
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(revokeAdminEndpointFormat) &&
            (revokeAdminEndpointFormat.Contains("revoke-admin") || revokeAdminEndpointFormat.Contains("revoke_admin") || revokeAdminEndpointFormat.Contains("revokeAdmin")))
        {
            revokeAdminEndpointFormat = "/admin/user/{0}/demote";
            changed = true;
        }

        if (changed)
            LogDebug("Normalized admin endpoint formats to match server routes (promote/demote).");
    }

    private string BuildFullUrl(string endpointFormat, int id)
    {
        try
        {
            var path = string.Format(endpointFormat, id);
            var baseUrl = ApiClient.BaseUrl ?? "";
            if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
            if (path.StartsWith("/")) return baseUrl.TrimEnd('/') + path;
            return baseUrl.TrimEnd('/') + "/" + path;
        }
        catch
        {
            return $"<invalid endpoint format: {endpointFormat}>";
        }
    }

    #region Stats & Users Loading

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
    }

    private void DisplayStats()
    {
        if (currentStats == null) return;
        if (userStatsText != null && currentStats.accounts != null)
            userStatsText.text = $"Total Users: {currentStats.accounts.totalUsers:N0}";
        if (clanStatsText != null && currentStats.clans != null)
            clanStatsText.text = $"Total Clans: {currentStats.clans.totalClans:N0}";
    }

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
    }

    private void ClearUserList()
    {
        if (userListParent == null) return;
        for (int i = userListParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(userListParent.GetChild(i).gameObject);
            else DestroyImmediate(userListParent.GetChild(i).gameObject);
        }
    }

    private void PopulateUserList()
    {
        if (userListParent == null || userPrefab == null || currentUsers?.users == null) return;

        foreach (var user in currentUsers.users)
        {
            var userItem = Instantiate(userPrefab, userListParent);
            var userItemScript = userItem.GetComponent<AdminUserItem>();
            if (userItemScript != null)
                userItemScript.SetupUser(user, this);
            else
                SetupUserItemFallback(userItem, user);
        }

        LogDebug($"Populated {currentUsers.users.Length} user items in scroll view");
    }

    private void SetupUserItemFallback(GameObject userItem, AdminUser user)
    {
        var usernameText = userItem.transform.Find("Name")?.GetComponent<TMP_Text>();
        var roleText = userItem.transform.Find("Role")?.GetComponent<TMP_Text>();
        if (usernameText != null) usernameText.text = user.username;
        if (roleText != null) roleText.text = user.role;
    }

    #endregion

    #region Edit User (PUT)

    // Accepts a payload dictionary containing only the fields to update.
    public void UpdateUserFieldsById(int userId, Dictionary<string, object> payload, Action<Dictionary<string, object>> onSuccess = null, Action<string> onError = null)
    {
        if (userId <= 0)
        {
            onError?.Invoke("Invalid user id");
            return;
        }

        if (payload == null || payload.Count == 0)
        {
            onError?.Invoke("No fields to update");
            return;
        }

        _ = UpdateUserFieldsByIdAsync(userId, payload, onSuccess, onError);
    }

    private async Task UpdateUserFieldsByIdAsync(int userId, Dictionary<string, object> payload, Action<Dictionary<string, object>> onSuccess, Action<string> onError)
    {
        try
        {
            string endpoint = string.Format(updateUserEndpointFormat, userId);
            LogDebug($"AdminController: PUT {endpoint} (fields: {payload.Count})");

            // Debug: log payload keys & values so we can see what is sent
            foreach (var kv in payload)
            {
                LogDebug($"Payload -> {kv.Key}: {kv.Value ?? "null"}");
            }

            // send request
            await ApiClient.PutAsync<Dictionary<string, object>, object>(endpoint, payload);

            onSuccess?.Invoke(payload);
            ShowStatus($"User {userId} updated", false);
        }
        catch (ApiException aex)
        {
            var msg = $"Failed to update user: {aex.StatusCode} {aex.Message}";
            onError?.Invoke(msg);
            ShowStatus(msg, true);
            Debug.LogError($"UpdateUserFieldsByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to update user: {ex.Message}";
            onError?.Invoke(msg);
            ShowStatus("Failed to update user", true);
            Debug.LogError($"UpdateUserFieldsByIdAsync error: {ex.Message}");
        }
    }

    // Convenience: update full AdminUser (sends fields that exist)
    public void UpdateUserById(AdminUser user, Action<AdminUser> onSuccess = null, Action<string> onError = null)
    {
        if (user == null) { onError?.Invoke("User is null"); return; }
        var payload = new Dictionary<string, object>
        {
            ["username"] = user.username,
            ["email"] = user.email,
            ["carrots"] = user.carrots,
            ["horseShoes"] = user.horseShoes,
            ["goldenCarrots"] = user.goldenCarrots
        };
        UpdateUserFieldsById(user.id, payload, returned => onSuccess?.Invoke(user), onError);
    }

    #endregion

    #region Admin Grant/Revoke (immediate vs confirmed)

    // Immediate helpers (no confirm) - used by edit panel to avoid double confirmation
    public void GrantAdminImmediateById(int userId, string username, Action onSuccess = null)
    {
        _ = ExecuteGrantAdminByIdAsync(userId, onSuccess);
    }

    public void RevokeAdminImmediateById(int userId, string username, Action onSuccess = null)
    {
        _ = ExecuteRevokeAdminByIdAsync(userId, onSuccess);
    }

    // Confirming variants (used by list buttons)
    public void RequestGrantAdminById(int userId, string username, Action onSuccess = null)
    {
        if (confirmPanel != null)
            confirmPanel.ShowGrantAdmin(username, () => { _ = ExecuteGrantAdminByIdAsync(userId, onSuccess); });
        else
            _ = ExecuteGrantAdminByIdAsync(userId, onSuccess);
    }

    public void RequestRevokeAdminById(int userId, string username, Action onSuccess = null)
    {
        if (confirmPanel != null)
            confirmPanel.ShowGrantAdmin(username, () => { _ = ExecuteRevokeAdminByIdAsync(userId, onSuccess); });
        else
            _ = ExecuteRevokeAdminByIdAsync(userId, onSuccess);
    }

    private async Task ExecuteGrantAdminByIdAsync(int userId, Action onSuccess)
    {
        try
        {
            string endpoint = string.Format(grantAdminEndpointFormat, userId);
            string fullUrl = BuildFullUrl(grantAdminEndpointFormat, userId);
            LogDebug($"AdminController: POST {endpoint} -> {fullUrl}");
            await ApiClient.PostAsync<object, object>(endpoint, null);
            onSuccess?.Invoke();

            // Refresh users list to reflect role change
            _ = LoadUsersListAsync();

            ShowStatus($"User {userId} granted admin", false);
        }
        catch (ApiException aex)
        {
            ShowStatus($"Failed to grant admin: {aex.StatusCode}", true);
            Debug.LogError($"ExecuteGrantAdminByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to grant admin", true);
            Debug.LogError($"ExecuteGrantAdminByIdAsync error: {ex.Message}");
        }
    }

    private async Task ExecuteRevokeAdminByIdAsync(int userId, Action onSuccess)
    {
        try
        {
            string endpoint = string.Format(revokeAdminEndpointFormat, userId);
            string fullUrl = BuildFullUrl(revokeAdminEndpointFormat, userId);
            LogDebug($"AdminController: POST {endpoint} -> {fullUrl}");
            await ApiClient.PostAsync<object, object>(endpoint, null);
            onSuccess?.Invoke();

            // Refresh users list to reflect role change
            _ = LoadUsersListAsync();

            ShowStatus($"User {userId} admin revoked", false);
        }
        catch (ApiException aex)
        {
            ShowStatus($"Failed to revoke admin: {aex.StatusCode}", true);
            Debug.LogError($"ExecuteRevokeAdminByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to revoke admin", true);
            Debug.LogError($"ExecuteRevokeAdminByIdAsync error: {ex.Message}");
        }
    }

    #endregion

    #region Ban / Unban / Delete

    public void RequestBanById(int userId, string username, Action onSuccess = null)
    {
        if (UserManager.GetCurrentUsername()?.Equals(username, StringComparison.OrdinalIgnoreCase) == true)
        {
            ShowStatus("Cannot ban yourself", true);
            return;
        }

        if (confirmPanel != null)
            confirmPanel.ShowBanUser(username, () => { _ = ExecuteBanByIdAsync(userId, onSuccess); });
        else
            _ = ExecuteBanByIdAsync(userId, onSuccess);
    }

    private async Task ExecuteBanByIdAsync(int userId, Action onSuccess)
    {
        try
        {
            string endpoint = string.Format(banEndpointFormat, userId);
            string fullUrl = BuildFullUrl(banEndpointFormat, userId);
            LogDebug($"AdminController: POST {endpoint} -> {fullUrl}");
            await ApiClient.PostAsync<object, object>(endpoint, null);
            onSuccess?.Invoke();

            // Refresh users list and stats because ban state changed
            _ = LoadUsersListAsync();
            _ = LoadAdminStatsAsync();

            ShowStatus($"User {userId} banned", false);
        }
        catch (ApiException aex)
        {
            ShowStatus($"Failed to ban user: {aex.StatusCode}", true);
            Debug.LogError($"ExecuteBanByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to ban user", true);
            Debug.LogError($"ExecuteBanByIdAsync error: {ex.Message}");
        }
    }

    public void RequestUnbanById(int userId, string username, Action onSuccess = null)
    {
        if (confirmPanel != null)
            confirmPanel.ShowBanUser(username, () => { _ = ExecuteUnbanByIdAsync(userId, onSuccess); });
        else
            _ = ExecuteUnbanByIdAsync(userId, onSuccess);
    }

    private async Task ExecuteUnbanByIdAsync(int userId, Action onSuccess)
    {
        try
        {
            string endpoint = string.Format(unbanEndpointFormat, userId);
            string fullUrl = BuildFullUrl(unbanEndpointFormat, userId);
            LogDebug($"AdminController: POST {endpoint} -> {fullUrl}");
            await ApiClient.PostAsync<object, object>(endpoint, null);
            onSuccess?.Invoke();

            // Refresh users list and stats because ban state changed
            _ = LoadUsersListAsync();
            _ = LoadAdminStatsAsync();

            ShowStatus($"User {userId} unbanned", false);
        }
        catch (ApiException aex)
        {
            ShowStatus($"Failed to unban user: {aex.StatusCode}", true);
            Debug.LogError($"ExecuteUnbanByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to unban user", true);
            Debug.LogError($"ExecuteUnbanByIdAsync error: {ex.Message}");
        }
    }

    public void RequestDeleteById(int userId, string username, Action onSuccess = null)
    {
        if (UserManager.GetCurrentUsername()?.Equals(username, StringComparison.OrdinalIgnoreCase) == true)
        {
            ShowStatus("Cannot delete yourself", true);
            return;
        }

        if (confirmPanel != null)
            confirmPanel.ShowDeleteUser(username, () => { _ = ExecuteDeleteByIdAsync(userId, onSuccess); });
        else
            _ = ExecuteDeleteByIdAsync(userId, onSuccess);
    }

    private async Task ExecuteDeleteByIdAsync(int userId, Action onSuccess)
    {
        try
        {
            string endpoint = string.Format(deleteEndpointFormat, userId);
            string fullUrl = BuildFullUrl(deleteEndpointFormat, userId);
            LogDebug($"AdminController: DELETE {endpoint} -> {fullUrl}");
            await ApiClient.DeleteAsync(endpoint);
            onSuccess?.Invoke();

            // refresh stats and list so userStatsText / clanStatsText update immediately
            _ = LoadAdminStatsAsync();
            _ = LoadUsersListAsync();

            ShowStatus($"User {userId} deleted", false);
        }
        catch (ApiException aex)
        {
            ShowStatus($"Failed to delete user: {aex.StatusCode}", true);
            Debug.LogError($"ExecuteDeleteByIdAsync API error: {aex.StatusCode} - {aex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus("Failed to delete user", true);
            Debug.LogError($"ExecuteDeleteByIdAsync error: {ex.Message}");
        }
    }

    #endregion

    #region Helpers used by AdminUserItem (wrappers)

    public void RequestBanToggle(AdminUser user, Action onSuccess = null)
    {
        if (user == null) return;
        if (user.isBanned)
            RequestUnbanById(user.id, user.username, () => { user.isBanned = false; onSuccess?.Invoke(); _ = LoadUsersListAsync(); });
        else
            RequestBanById(user.id, user.username, () => { user.isBanned = true; onSuccess?.Invoke(); _ = LoadUsersListAsync(); });
    }

    public void RequestDeleteUser(AdminUser user, Action onSuccess = null)
    {
        if (user == null) return;
        RequestDeleteById(user.id, user.username, () =>
        {
            onSuccess?.Invoke();
            // ensure stats and list refreshed (in case caller didn't rely on callbacks)
            _ = LoadAdminStatsAsync();
            _ = LoadUsersListAsync();
        });
    }

    public void RequestToggleAdmin(AdminUser user, Action onSuccess = null)
    {
        if (user == null) return;
        bool currentlyAdmin = user.role == "Admin";
        if (currentlyAdmin)
            RequestRevokeAdminById(user.id, user.username, () => { user.role = "User"; onSuccess?.Invoke(); _ = LoadUsersListAsync(); });
        else
            RequestGrantAdminById(user.id, user.username, () => { user.role = "Admin"; onSuccess?.Invoke(); _ = LoadUsersListAsync(); });
    }

    // Open edit panel and optionally subscribe to save event to refresh UI
    public void OpenEditUser(AdminUser user, Action<AdminUser> onSaved = null)
    {
        if (user == null) return;
        if (editUserPanelRoot != null)
        {
            editUserPanelRoot.SetActive(true);
            LogDebug($"OpenEditUser: opening edit panel for user {user.username} (id {user.id})");
            var editComp = editUserPanelRoot.GetComponent<EditUserAdmin>();
            if (editComp != null)
            {
                if (onSaved != null)
                {
                    // subscribe with self-unsubscribe to avoid leaking handlers
                    Action<AdminUser> handler = null;
                    handler = (u) =>
                    {
                        try { onSaved?.Invoke(u); }
                        finally { editComp.OnUserSaved -= handler; }
                    };
                    editComp.OnUserSaved += handler;
                }

                editComp.Open(user);
            }
        }
        else
        {
            LogDebug($"OpenEditUser requested for user {user.username} but editUserPanelRoot is not assigned.");
        }
    }

    #endregion
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