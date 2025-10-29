using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Button editButton;      // back to Edit (opens edit panel)
    [SerializeField] private Button deleteButton;

    private AdminUser userData;
    private AdminController adminController; // set by AdminController when populating list

    // Check if the current logged-in user is viewing their own profile
    private bool IsCurrentUser()
    {
        if (userData == null) return false;

        string currentUsername = UserManager.GetCurrentUsername();
        return !string.IsNullOrEmpty(currentUsername) &&
               currentUsername.Equals(userData.username, System.StringComparison.OrdinalIgnoreCase);
    }

    // New signature: AdminController will provide itself so actions are performed there.
    public void SetupUser(AdminUser user, AdminController controller)
    {
        userData = user;
        adminController = controller;
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

        if (bannedStatusText != null)
            bannedStatusText.text = userData.isBanned ? "Banned" : "";
    }

    private void SetupButtons()
    {
        // Remove previous listeners to avoid duplicates
        banButton?.onClick.RemoveAllListeners();
        editButton?.onClick.RemoveAllListeners();
        deleteButton?.onClick.RemoveAllListeners();

        // Wire buttons to forward actions to AdminController.
        if (banButton != null)
            banButton.onClick.AddListener(() => OnBanClicked());
        if (editButton != null)
            editButton.onClick.AddListener(() => OnEditClicked());
        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => OnDeleteClicked());

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
                banButtonText.color = Color.white;
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

        if (editButton != null)
        {
            // Edit button opens edit panel; disable for self to match previous UX (change if you want to allow self-edit)
            editButton.interactable = !isCurrentUser;
            var editText = editButton.GetComponentInChildren<TMP_Text>();
            if (editText != null)
            {
                editText.text = "Edit";
                editText.color = isCurrentUser ? Color.gray : Color.white;
            }
        }
    }

    private void OnBanClicked()
    {
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot ban yourself!");
            return;
        }

        if (adminController != null)
        {
            adminController.RequestBanToggle(userData, () =>
            {
                // Update local display after controller completes action
                UpdateDisplay();
                UpdateButtonStates();
            });
        }
        else
        {
            Debug.LogWarning("AdminController not assigned to AdminUserItem. Action skipped.");
        }
    }

    private void OnDeleteClicked()
    {
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot delete yourself!");
            return;
        }

        if (adminController != null)
        {
            adminController.RequestDeleteUser(userData, () =>
            {
                // remove prefab after successful delete
                Destroy(gameObject);
            });
        }
        else
        {
            Debug.LogWarning("AdminController not assigned to AdminUserItem. Action skipped.");
        }
    }

    private void OnGrantAdminClicked()
    {
        // kept in case you still use grant/revoke from code; not wired to editButton anymore
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot change your own admin status here!");
            return;
        }

        if (adminController != null)
        {
            adminController.RequestToggleAdmin(userData, () =>
            {
                // update local model/state after change
                userData.role = userData.role == "Admin" ? "User" : "Admin";
                UpdateDisplay();
                UpdateButtonStates();
            });
        }
        else
        {
            Debug.LogWarning("AdminController not assigned to AdminUserItem. Action skipped.");
        }
    }

    private void OnEditClicked()
    {
        if (IsCurrentUser())
        {
            Debug.LogWarning("Cannot edit yourself from this panel!");
            return;
        }

        if (adminController != null)
        {
            adminController.OpenEditUser(userData); // controller should open the edit panel (you will implement)
        }
        else
        {
            Debug.LogWarning("AdminController not assigned to AdminUserItem. Action skipped.");
        }
    }

    // Public method to get user data
    public AdminUser GetUserData()
    {
        return userData;
    }
}