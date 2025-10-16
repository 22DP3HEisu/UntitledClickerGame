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
    [SerializeField] private Button deleteButton;
    
    private AdminUser userData;
    
    /// <summary>
    /// Check if the current logged-in user is viewing their own profile
    /// </summary>
    /// <returns>True if current user is viewing themselves</returns>
    private bool IsCurrentUser()
    {
        if (userData == null) return false;
        
        string currentUsername = UserManager.GetCurrentUsername();
        return !string.IsNullOrEmpty(currentUsername) && 
               currentUsername.Equals(userData.username, System.StringComparison.OrdinalIgnoreCase);
    }
    
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
        deleteButton?.onClick.AddListener(() => DeleteUser());
        
        // Update button states based on user data
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        if (userData == null) return;
        
        bool isCurrentUser = IsCurrentUser();
        
        if (banButton != null)
        {
            var banButtonText = banButton.GetComponentInChildren<TMP_Text>();
            if (banButtonText != null)
            {
                banButtonText.text = userData.isBanned ? "Unban" : "Ban";
            }
            
            // Disable ban button if user is trying to ban themselves
            banButton.interactable = !isCurrentUser;
            
            // Visual feedback for disabled state
            if (isCurrentUser && banButtonText != null)
            {
                banButtonText.color = Color.gray;
                banButtonText.text = "Can't Ban Self";
            }
        }
        
        if (deleteButton != null)
        {
            // Disable delete button if user is trying to delete themselves
            deleteButton.interactable = !isCurrentUser;
            
            // Visual feedback for disabled state
            var deleteButtonText = deleteButton.GetComponentInChildren<TMP_Text>();
            if (isCurrentUser && deleteButtonText != null)
            {
                deleteButtonText.color = Color.gray;
                deleteButtonText.text = "Can't Delete Self";
            }
        }
    }
    
    private async void ToggleBanUser()
    {
        // Prevent users from banning themselves
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot ban yourself!");
            return;
        }
        
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
        // Prevent users from deleting themselves
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot delete yourself!");
            return;
        }
        
        // Show confirmation dialog (you might want to implement a proper confirmation UI)
        // if (Application.isEditor || Debug.isDebugBuild)
        // {
        //     Debug.LogWarning($"Delete user {userData.username} requested - implement confirmation dialog");
        //     return;
        // }

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