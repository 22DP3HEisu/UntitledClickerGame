using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditUserAdmin : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot; // root to show/hide the edit panel

    [Header("Labels (optional)")]
    [SerializeField] private TMP_Text UsernameLabel;
    [SerializeField] private TMP_Text EmailLabel;
    [SerializeField] private TMP_Text CarrotsLabel;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField UsernameInput;
    [SerializeField] private TMP_InputField EmailInput;
    [SerializeField] private TMP_InputField CarrotsInput;
    [SerializeField] private Toggle isAdminToggle;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;

    [Header("Controller")]
    [SerializeField] private AdminController adminController; // assign if you want automatic grant/revoke calls

    private AdminUser currentUser;

    // Event fired after user pressed Save and update flow completed (local model updated).
    public event Action<AdminUser> OnUserSaved;

    private void Awake()
    {
        // Wire buttons if assigned
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (saveButton != null) saveButton.onClick.RemoveListener(OnSaveClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
    }

    // Open the panel and populate fields from an AdminUser instance
    public void Open(AdminUser user)
    {
        if (user == null) return;
        currentUser = user;

        if (panelRoot != null) panelRoot.SetActive(true);

        if (UsernameInput != null) UsernameInput.text = user.username ?? string.Empty;
        if (EmailInput != null) EmailInput.text = user.email ?? string.Empty;
        if (CarrotsInput != null) CarrotsInput.text = user.carrots.ToString();
        if (isAdminToggle != null) isAdminToggle.isOn = string.Equals(user.role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    // Close/hide panel
    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentUser = null;
    }

    // Save button handler: updates local model and optionally calls AdminController to grant/revoke admin.
    private void OnSaveClicked()
    {
        if (currentUser == null)
        {
            Debug.LogWarning("EditUserAdmin: no user assigned");
            return;
        }

        // Update local fields
        if (UsernameInput != null) currentUser.username = UsernameInput.text.Trim();
        if (EmailInput != null) currentUser.email = EmailInput.text.Trim();

        if (CarrotsInput != null)
        {
            if (int.TryParse(CarrotsInput.text, out int carrots))
                currentUser.carrots = carrots;
            else
                Debug.LogWarning("EditUserAdmin: invalid carrots input");
        }

        bool wantAdmin = isAdminToggle != null && isAdminToggle.isOn;
        bool isCurrentlyAdmin = string.Equals(currentUser.role, "Admin", StringComparison.OrdinalIgnoreCase);

        // If AdminController assigned, use its grant/revoke helpers for admin toggle
        if (adminController != null)
        {
            if (wantAdmin && !isCurrentlyAdmin)
            {
                adminController.RequestGrantAdminById(currentUser.id, currentUser.username, () =>
                {
                    currentUser.role = "Admin";
                    OnUserSaved?.Invoke(currentUser);
                });
            }
            else if (!wantAdmin && isCurrentlyAdmin)
            {
                adminController.RequestRevokeAdminById(currentUser.id, currentUser.username, () =>
                {
                    currentUser.role = "User";
                    OnUserSaved?.Invoke(currentUser);
                });
            }
            else
            {
                // No admin state change required — just invoke saved event
                OnUserSaved?.Invoke(currentUser);
            }
        }
        else
        {
            // No controller: just update local model and notify listeners
            currentUser.role = wantAdmin ? "Admin" : "User";
            OnUserSaved?.Invoke(currentUser);
        }

        Close();
    }
}