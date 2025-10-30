using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Linq;

public class ProfilePanelCode : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_Text carrotsText;
    [SerializeField] private TMP_Text buildingsText;
    [SerializeField] private TMP_Text upgradesText;
    [SerializeField] private TMP_Text achievementsText;
    [SerializeField] private TMP_Text clanText;
    [SerializeField] private TMP_Text statusText;

    [Header("Actions")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button submitButton;

    [Header("Visual Elements")]
    [SerializeField] private GameObject loadingIndicator;

    private UserProfileResponse currentProfile;
    private int userId;

    [Serializable]
    public class BuildingsResponse
    {
        public string message;
        public BuildingData[] buildings;
    }

    [Serializable]
    public class BuildingData
    {
        public string name;
        public int count;
        public string firstPurchased;
    }

    [Serializable]
    public class UpdateProfileRequest
    {
        public string username;
        public string email;
    }

    private void Awake()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        // fire-and-forget is OK here; method handles its own errors and UI.
        _ = LoadProfileInfo();
    }

    private async Task LoadProfileInfo()
    {
        ShowLoading(true);
        ShowStatus("Loading profile...", false);

        // Try to read a saved user id (optional); do NOT block loading if missing.
        userId = PlayerPrefs.GetInt("RegisteredUserId", -1);
        if (userId == -1)
        {
            Debug.Log("[ProfilePanelCode] No saved RegisteredUserId in PlayerPrefs - will use auth token to fetch profile if available.");
        }

        // Require a valid auth token for server profile fetch.
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Auth token missing or expired. Please log in again.", true);
            ShowLoading(false);
            return;
        }

        try
        {
            // Use authenticated endpoint that returns currently logged-in user's profile.
            var response = await ApiClient.GetAsync<UserProfileResponse>("/user");

            if (response != null && response.user != null)
            {
                currentProfile = response;
                await UpdateDisplay();
                ShowStatus("Profile loaded successfully", false);

                // Persist basic info to PlayerPrefs for convenience (optional)
                PlayerPrefs.SetString("RegisteredUsername", currentProfile.user.username ?? "");
                PlayerPrefs.SetString("RegisteredEmail", currentProfile.user.email ?? "");
                PlayerPrefs.Save();
            }
            else
            {
                ShowStatus("Profile not found. Showing test data.", true);
                LoadMockProfile();
                await UpdateDisplay();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ProfilePanelCode] Error fetching profile: {ex.Message}");
            ShowStatus("Could not load profile (using mock data).", true);
            LoadMockProfile();
            await UpdateDisplay();
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async Task UpdateDisplay()
    {
        if (currentProfile?.user == null) return;

        var user = currentProfile.user;

        // --- Basic info ---
        if (usernameInputField) usernameInputField.text = user.username;
        if (emailInputField) emailInputField.text = user.email;
        if (carrotsText) carrotsText.text = $"{user.gameData?.carrots ?? 0}";

        // --- Upgrades ---
        if (upgradesText)
        {
            int purchasedUpgrades = 0;
            int totalUpgrades = 0;

            if (PassiveUpgradeManager.Instance != null)
            {
                var upgrades = PassiveUpgradeManager.Instance.GetAllUpgrades();
                totalUpgrades = upgrades.Count;
                purchasedUpgrades = upgrades.Count(u => u.isPurchased);
            }

            upgradesText.text = $"{purchasedUpgrades} / {totalUpgrades}";
        }

        // --- Buildings ---
        if (buildingsText)
        {
            try
            {
                // Fetch buildings from server
                var buildingsResponse = await ApiClient.GetAsync<BuildingsResponse>("/user/buildings");
                int unlockedBuildings = buildingsResponse?.buildings?.Length ?? 0;

                // Total buildings from PassiveUpgradeManager (all possible upgrades)
                int totalBuildings = 0;
                if (PassiveUpgradeManager.Instance != null)
                {
                    var allUpgrades = PassiveUpgradeManager.Instance.GetAllUpgrades();
                    totalBuildings = allUpgrades.Count;
                }

                // Show x/y format
                buildingsText.text = $"{unlockedBuildings} / {totalBuildings}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading buildings count: {ex.Message}");
                buildingsText.text = "0 / 0";
            }
        }

        // --- Achievements ---
        if (achievementsText)
        {
            int completedCount = 0;
            int totalCount = 0;

            if (AchievementManager.Instance != null)
            {
                completedCount = AchievementManager.Instance.GetCompletedAchievements().Count;
                totalCount = AchievementManager.Instance.GetAllAchievements().Count;
            }

            achievementsText.text = $"{completedCount} / {totalCount}";
        }

        // --- Clan ---
        if (clanText)
            clanText.text = "Test";
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

    private void SetupButtons()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => { _ = LoadProfileInfo(); });
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            // Ensure async listener doesn't block Unity event system
            submitButton.onClick.AddListener(() => { _ = SubmitProfileChanges(); });
        }
    }

    private async Task SubmitProfileChanges()
    {
        if (!ApiClient.IsTokenValid())
        {
            ShowStatus("Cannot update profile: not logged in.", true);
            return;
        }

        if (currentProfile?.user == null)
        {
            ShowStatus("No profile loaded.", true);
            return;
        }

        string newUsername = usernameInputField?.text.Trim() ?? "";
        string newEmail = emailInputField?.text.Trim() ?? "";

        Debug.Log($"Submitting profile: username='{newUsername}', email='{newEmail}'");

        if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newEmail))
        {
            ShowStatus("Username and email cannot be empty.", true);
            return;
        }

        ShowLoading(true);
        ShowStatus("Submitting profile changes...", false);

        try
        {
            var requestData = new UpdateProfileRequest
            {
                username = newUsername,
                email = newEmail
            };

            var response = await ApiClient.PutAsync<UpdateProfileRequest, UserProfileResponse>("/user/update", requestData);

            if (response != null && response.user != null)
            {
                // Preserve gameData
                response.user.gameData = currentProfile.user.gameData;

                currentProfile = response;
                await UpdateDisplay();
                ShowStatus("Profile updated successfully.", false);

                PlayerPrefs.SetString("RegisteredUsername", newUsername);
                PlayerPrefs.SetString("RegisteredEmail", newEmail);
                PlayerPrefs.Save();
            }
            else
            {
                ShowStatus("Failed to update profile.", true);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[ProfilePanelCode] SubmitProfileChanges Exception: {ex.Message}");
            ShowStatus("Error updating profile.", true);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    // Mock data fallback so UI always shows something
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