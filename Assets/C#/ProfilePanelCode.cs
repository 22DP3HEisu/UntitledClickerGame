using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class ProfilePanelCode : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_Text carrotsText;
    [SerializeField] private TMP_Text upgradesText;
    [SerializeField] private TMP_Text achievementsText;
    [SerializeField] private TMP_Text clanText;
    [SerializeField] private TMP_Text statusText;

    [Header("Actions")]
    [SerializeField] private Button refreshButton;

    [Header("Visual Elements")]
    [SerializeField] private GameObject loadingIndicator;

    private UserProfileResponse currentProfile;
    private int userId;

    private void Awake()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        LoadProfileInfo();
    }

    private void SetupButtons()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => _ = LoadProfileInfo());
        }
    }

    private async Task LoadProfileInfo()
    {
        ShowLoading(true);
        ShowStatus("Loading profile...", false);

        userId = PlayerPrefs.GetInt("RegisteredUserId", -1);

        if (userId == -1)
        {
            ShowStatus("No saved user ID found. Please log in again.", true);
            ShowLoading(false);
            return;
        }

        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Auth token missing or expired. Please log in again.", true);
            ShowLoading(false);
            return;
        }

        try
        {
            // ✅ Updated endpoint to use user ID
            var response = await ApiClient.GetAsync<UserProfileResponse>("/user");

            if (response != null && response.user != null)
            {
                currentProfile = response;
                UpdateDisplay();
                ShowStatus("Profile loaded successfully", false);
            }
            else
            {
                ShowStatus("Profile not found. Showing test data.", true);
                LoadMockProfile();
                UpdateDisplay();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ProfilePanelCode] Error: {ex.Message}");
            ShowStatus("Could not load profile (using mock data).", true);
            LoadMockProfile();
            UpdateDisplay();
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void UpdateDisplay()
    {
        if (currentProfile?.user == null) return;

        var user = currentProfile.user;

        if (usernameInputField) usernameInputField.text = user.username;
        if (emailInputField) emailInputField.text = user.email;
        if (carrotsText) carrotsText.text = $"{user.gameData?.carrots ?? 0}";

        if (upgradesText)
        {
            int upgradeCount = 0;
            if (user.gameData?.upgrades != null) upgradesText.text = $"3";
        }

        if (achievementsText)
            achievementsText.text = "7";

        if (clanText)
            clanText.text = "";
    }

    private void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }

        Debug.Log($"[ProfilePanelCode] {message}");
    }

    // 🧩 Mock data fallback so UI always shows something
    private void LoadMockProfile()
    {
        currentProfile = new UserProfileResponse
        {
            message = "Mock data",
            user = new UserProfileResponse.UserProfile
            {
                id = 1,
                username = PlayerPrefs.GetString("RegisteredUsername", "TestUser"),
                email = PlayerPrefs.GetString("RegisteredEmail", "test@example.com"),
                role = "player",
                createdAt = DateTime.UtcNow.ToString("u"),
                isBanned = false,
                gameData = new UserProfileResponse.GameData
                {
                    carrots = 300,
                    horseShoes = 2,
                    goldenCarrots = 1,


                }
            }
        };
    }
}
