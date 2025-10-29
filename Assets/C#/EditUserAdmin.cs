using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditUserAdmin : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

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
    [SerializeField] private AdminController adminController;

    private AdminUser currentUser;
    // snapshot of original values so we send only changed fields
    private string originalUsername;
    private string originalEmail;
    private int originalCarrots;
    private bool originalIsAdmin;

    // Event fired after user pressed Save and update flow completed (local model updated).
    public event Action<AdminUser> OnUserSaved;

    // Prevent duplicate saves while an async update is in progress
    private bool isSaving;

    private void Awake()
    {
        // Wire buttons if assigned
        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        if (panelRoot != null) panelRoot.SetActive(false);
        isSaving = false;
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

        // take snapshot of original values
        originalUsername = user.username ?? string.Empty;
        originalEmail = user.email ?? string.Empty;
        originalCarrots = user.carrots;
        originalIsAdmin = string.Equals(user.role, "Admin", StringComparison.OrdinalIgnoreCase);

        if (panelRoot != null) panelRoot.SetActive(true);

        if (UsernameInput != null) UsernameInput.text = originalUsername;
        if (EmailInput != null) EmailInput.text = originalEmail;
        if (CarrotsInput != null) CarrotsInput.text = originalCarrots.ToString();
        if (isAdminToggle != null) isAdminToggle.isOn = originalIsAdmin;

        // ensure buttons are interactable when opened
        if (saveButton != null) saveButton.interactable = true;
        isSaving = false;
    }

    // Close/hide panel
    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        currentUser = null;
        isSaving = false;
        if (saveButton != null) saveButton.interactable = true;
    }

    // Save button handler: compute changed fields and call AdminController.UpdateUserFieldsById.
    private void OnSaveClicked()
    {
        if (currentUser == null)
        {
            Debug.LogWarning("EditUserAdmin: no user assigned");
            return;
        }

        if (isSaving)
        {
            Debug.LogWarning("EditUserAdmin: save already in progress");
            return;
        }

        // fetch values from inputs
        string newUsername = UsernameInput != null ? UsernameInput.text.Trim() : originalUsername;
        string newEmail = EmailInput != null ? EmailInput.text.Trim() : originalEmail;
        int newCarrots = originalCarrots;
        if (CarrotsInput != null)
        {
            if (!int.TryParse(CarrotsInput.text, out newCarrots))
            {
                Debug.LogWarning("EditUserAdmin: invalid carrots input");
                // Keep originalCarrots if parse fails
                newCarrots = originalCarrots;
            }
        }
        bool wantAdmin = isAdminToggle != null && isAdminToggle.isOn;

        // build payload with only changed fields
        var payload = new Dictionary<string, object>();

        if (!string.Equals(newUsername, originalUsername, StringComparison.Ordinal))
            payload["username"] = newUsername;

        if (!string.Equals(newEmail, originalEmail, StringComparison.Ordinal))
            payload["email"] = newEmail;

        if (newCarrots != originalCarrots)
            payload["carrots"] = newCarrots;

        // If no payload changes, still may need admin change. Handle admin separately.
        bool adminChanged = wantAdmin != originalIsAdmin;

        // If there are fields to update, call UpdateUserFieldsById; otherwise skip to admin change.
        if (payload.Count > 0)
        {
            if (adminController == null)
            {
                Debug.LogWarning("EditUserAdmin: adminController not assigned; changes will not be persisted to server.");
                // apply locally
                currentUser.username = newUsername;
                currentUser.email = newEmail;
                currentUser.carrots = newCarrots;
                // handle admin toggle below
                ApplyAdminChangeIfNeeded(adminChanged, wantAdmin);
                OnUserSaved?.Invoke(currentUser);
                Close();
                return;
            }

            // mark saving and disable save button to avoid race where panel is closed before callback runs
            isSaving = true;
            if (saveButton != null) saveButton.interactable = false;

            adminController.UpdateUserFieldsById(currentUser.id, payload,
                onSuccess: (returnedPayload) =>
                {
                    try
                    {
                        // Update local model with changed fields
                        if (returnedPayload.ContainsKey("username")) currentUser.username = newUsername;
                        if (returnedPayload.ContainsKey("email")) currentUser.email = newEmail;
                        if (returnedPayload.ContainsKey("carrots")) currentUser.carrots = newCarrots;

                        // After successfully saving fields, apply admin change immediately without confirm
                        ApplyAdminChangeIfNeeded(adminChanged, wantAdmin);

                        OnUserSaved?.Invoke(currentUser);

                        // Close panel now that update completed
                        Close();
                    }
                    finally
                    {
                        // Ensure saving flag is cleared and buttons re-enabled
                        isSaving = false;
                        if (saveButton != null) saveButton.interactable = true;
                    }
                },
                onError: (err) =>
                {
                    Debug.LogError($"EditUserAdmin: update failed - {err}");
                    // Keep panel open so user can retry. Re-enable save.
                    isSaving = false;
                    if (saveButton != null) saveButton.interactable = true;
                });

            // do not close here — wait for async callback to finish
            return;
        }
        else
        {
            // No profile fields changed; only admin toggle maybe changed
            ApplyAdminChangeIfNeeded(adminChanged, wantAdmin);
            OnUserSaved?.Invoke(currentUser);
            Close();
        }
    }

    private void ApplyAdminChangeIfNeeded(bool adminChanged, bool wantAdmin)
    {
        if (!adminChanged || adminController == null || currentUser == null) return;

        // Capture a strong reference to the AdminUser instance so callbacks don't reference the possibly-cleared 'currentUser' field.
        var userRef = currentUser;

        if (wantAdmin && !string.Equals(userRef.role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            adminController.GrantAdminImmediateById(userRef.id, userRef.username, () =>
            {
                // Update the captured user reference safely (works even if this EditUserAdmin instance closed)
                try
                {
                    userRef.role = "Admin";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GrantAdmin callback failed to update local model: {ex.Message}");
                }
            });
        }
        else if (!wantAdmin && string.Equals(userRef.role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            adminController.RevokeAdminImmediateById(userRef.id, userRef.username, () =>
            {
                try
                {
                    userRef.role = "User";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"RevokeAdmin callback failed to update local model: {ex.Message}");
                }
            });
        }
    }
}