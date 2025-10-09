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
    [SerializeField] private Button promoteButton;
    [SerializeField] private Button demoteButton;
    [SerializeField] private Button banButton;
    [SerializeField] private Button deleteButton;
    
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
        if (emailText != null) emailText.text = userData.email;
        if (roleText != null) 
        {
            roleText.text = userData.role;
            // Color code roles
            roleText.color = userData.role == "Admin" ? Color.red : Color.white;
        }
        
        // Currency display
        if (carrotsText != null) carrotsText.text = userData.carrots.ToString("N0");
        if (horseShoesText != null) horseShoesText.text = userData.horseShoes.ToString("N0");
        if (goldenCarrotsText != null) goldenCarrotsText.text = userData.goldenCarrots.ToString("N0");
        
        // Dates
        if (createdAtText != null) 
        {
            if (System.DateTime.TryParse(userData.createdAt, out System.DateTime createdDate))
            {
                createdAtText.text = createdDate.ToString("MM/dd/yyyy");
            }
            else
            {
                createdAtText.text = userData.createdAt;
            }
        }
        
        // Ban status
        if (bannedStatusText != null)
        {
            bannedStatusText.text = userData.isBanned ? "BANNED" : "Active";
            bannedStatusText.color = userData.isBanned ? Color.red : Color.green;
        }
    }
    
    private void SetupButtons()
    {
        // Setup button listeners
        promoteButton?.onClick.AddListener(() => PromoteUser());
        demoteButton?.onClick.AddListener(() => DemoteUser());
        banButton?.onClick.AddListener(() => ToggleBanUser());
        deleteButton?.onClick.AddListener(() => DeleteUser());
        
        // Update button states based on user data
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        if (userData == null) return;
        
        // Show/hide buttons based on current role and status
        if (promoteButton != null) 
            promoteButton.gameObject.SetActive(userData.role != "Admin");
        
        if (demoteButton != null) 
            demoteButton.gameObject.SetActive(userData.role == "Admin");
        
        if (banButton != null)
        {
            var banButtonText = banButton.GetComponentInChildren<TMP_Text>();
            if (banButtonText != null)
            {
                banButtonText.text = userData.isBanned ? "Unban" : "Ban";
            }
        }
    }
    
    private async void PromoteUser()
    {
        try
        {
            await ApiClient.PostAsync<object, object>($"/admin/user/{userData.id}/promote", null);
            userData.role = "Admin";
            UpdateDisplay();
            UpdateButtonStates();
            Debug.Log($"User {userData.username} promoted to Admin");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to promote user: {ex.Message}");
        }
    }
    
    private async void DemoteUser()
    {
        try
        {
            await ApiClient.PostAsync<object, object>($"/admin/user/{userData.id}/demote", null);
            userData.role = "User";
            UpdateDisplay();
            UpdateButtonStates();
            Debug.Log($"User {userData.username} demoted to User");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to demote user: {ex.Message}");
        }
    }
    
    private async void ToggleBanUser()
    {
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
    
    // Public method to get user data
    public AdminUser GetUserData()
    {
        return userData;
    }
}