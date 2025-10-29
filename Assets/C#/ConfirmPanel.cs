using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private TMP_Text yesButtonText;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text noButtonText;

    [Header("Defaults")]
    [SerializeField] private string defaultYesText = "Yes";
    [SerializeField] private string defaultNoText = "No";

    private Action onYes;
    private Action onNo;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // keep buttons wired to internal handlers so we don't need to remove inspector listeners
        if (yesButton != null)
            yesButton.onClick.AddListener(InternalYes);

        if (noButton != null)
            noButton.onClick.AddListener(InternalNo);
    }

    private void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(InternalYes);
        if (noButton != null)
            noButton.onClick.RemoveListener(InternalNo);
    }

    // Show generic confirm panel. Callbacks are invoked after the panel closes.
    // yesLabel/noLabel optional override button labels.
    public void Show(string message, Action yesCallback, Action noCallback = null, string yesLabel = null, string noLabel = null)
    {
        if (panelRoot == null) return;

        onYes = yesCallback;
        onNo = noCallback;

        if (messageText != null) messageText.text = message ?? string.Empty;

        if (yesButtonText != null) yesButtonText.text = string.IsNullOrEmpty(yesLabel) ? defaultYesText : yesLabel;
        if (noButtonText != null) noButtonText.text = string.IsNullOrEmpty(noLabel) ? defaultNoText : noLabel;

        panelRoot.SetActive(true);

        // optional: set navigation / focus to yesButton
        if (yesButton != null)
            yesButton.Select();
    }

    // Convenience helpers for common admin actions
    public void ShowDeleteUser(string username, Action confirm)
    {
        Show($"Delete user '{username}'?", confirm, null, "Delete", "Cancel");
    }

    public void ShowBanUser(string username, Action confirm)
    {
        Show($"Ban user '{username}'?", confirm, null, "Ban", "Cancel");
    }

    public void ShowGrantAdmin(string username, Action confirm)
    {
        Show($"Grant admin to '{username}'?", confirm, null, "Grant", "Cancel");
    }

    public void Hide()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
        // clear callbacks to avoid accidental reuse
        onYes = null;
        onNo = null;
    }

    private void InternalYes()
    {
        try
        {
            onYes?.Invoke();
        }
        finally
        {
            Hide();
        }
    }

    private void InternalNo()
    {
        try
        {
            onNo?.Invoke();
        }
        finally
        {
            Hide();
        }
    }
}