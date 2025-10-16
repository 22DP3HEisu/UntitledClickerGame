using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for individual user items in the admin users list
/// Attach this to your user prefab and assign the UI components
/// </summary>
public class AdminUserItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text carrotsText;
    [SerializeField] private TMP_Text horseShoesText;
    [SerializeField] private TMP_Text goldenCarrotsText;
    [SerializeField] private TMP_Text createdAtText;
    [SerializeField] private TMP_Text bannedStatusText;
    
    [Header("Actions")]
    [SerializeField] private Button banButton;
    [SerializeField] private Button editButton;
    
    private AdminUser userData;
    
    public void SetupUser(AdminUser user)
    {
        userData = user;
        UpdateDisplay();
        SetupButtons();
    }
    
    private void UpdateDisplay()
    {
        if (userData == null) return;
        
        // Set text fields
        if (usernameText != null) usernameText.text = userData.username;
        if (roleText != null) 
        {
            roleText.text = userData.role;
            // Color code roles
            roleText.color = userData.role == "Admin" ? Color.red : Color.white;
        }
    }
    
    private void SetupButtons()
    {
        // Setup button listeners
        banButton?.onClick.AddListener(() => ToggleBanUser());
        editButton?.onClick.AddListener(() => EditUser());
        
        // Update button states based on user data
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        if (userData == null) return;
        
        if (banButton != null)
        {
            var banButtonText = banButton.GetComponentInChildren<TMP_Text>();
            if (banButtonText != null)
            {
                banButtonText.text = userData.isBanned ? "Unban" : "Ban";
            }
        }
    }
    
    private async void ToggleBanUser()
    {
        Debug.Log($"Toggling ban status for user {userData.username}...");
        try
        {
            string endpoint = userData.isBanned ? "unban" : "ban";
            await ApiClient.PostAsync<object, object>($"/admin/user/{userData.id}/{endpoint}", null);
            userData.isBanned = !userData.isBanned;
            UpdateDisplay();
            UpdateButtonStates();
            Debug.Log($"User {userData.username} {(userData.isBanned ? "banned" : "unbanned")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to toggle ban status: {ex.Message}");
        }
    }

    private async void DeleteUser()
    {
        // Show confirmation dialog (you might want to implement a proper confirmation UI)
        if (Application.isEditor || Debug.isDebugBuild)
        {
            Debug.LogWarning($"Delete user {userData.username} requested - implement confirmation dialog");
            return;
        }

        try
        {
            await ApiClient.DeleteAsync($"/admin/user/{userData.id}");
            Destroy(gameObject);
            Debug.Log($"User {userData.username} deleted");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to delete user: {ex.Message}");
        }
    }
    
    private void EditUser()
    {
        // Open edit user dialog or scene
        Debug.Log($"Edit user {userData.username} requested - implement edit functionality");
    }
    
    // Public method to get user data
    public AdminUser GetUserData()
    {
        return userData;
    }
}