using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads and displays a leaderboard sorted from most carrots to least.
/// Attach this to a leaderboard manager object and assign the list parent and entry prefab.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform leaderboardListParent;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button carrotsHeaderButton;
    [SerializeField] private Button backButton;
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private string gameSceneName = "game";

    private LeaderboardResponse currentLeaderboard;
    private bool sortDescending = true; // Default: most carrots first

    private void Start()
    {
        SetupUI();
        _ = LoadLeaderboardAsync();
    }

    private void SetupUI()
    {
        if (carrotsHeaderButton != null)
        {
            carrotsHeaderButton.onClick.AddListener(ToggleSortOrder);
            UpdateSortButtonText();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    public async Task LoadLeaderboardAsync()
    {
        ShowStatus("Loading leaderboard...", false);

        try
        {
            ClearList();

            var response = await ApiClient.GetAsync<LeaderboardResponse>("/leaderboard?limit=100");

            if (response?.entries != null)
            {
                // Store unsorted data, then apply current sort
                currentLeaderboard = response;
                ApplySortAndDisplay();
                ShowStatus($"Loaded {response.entries.Length} players", false);
                LogDebug($"Leaderboard loaded: {response.entries.Length} entries");
            }
            else
            {
                ShowStatus("Failed to load leaderboard", true);
                LogDebug("Leaderboard response was null or contained no entries");
            }
        }
        catch (ApiException ex)
        {
            ShowStatus($"Server error: {ex.Message}", true);
            LogDebug($"Leaderboard API error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowStatus($"Error loading leaderboard: {ex.Message}", true);
            LogDebug($"Leaderboard error: {ex.Message}");
        }
    }

    private void PopulateLeaderboard()
    {
        if (leaderboardListParent == null || leaderboardEntryPrefab == null || currentLeaderboard?.entries == null) return;

        ClearList();

        for (int i = 0; i < currentLeaderboard.entries.Length; i++)
        {
            var entry = currentLeaderboard.entries[i];
            GameObject obj = Instantiate(leaderboardEntryPrefab, leaderboardListParent);

            // Set TMP_Text children named "Name", "Carrots", "Rank"
            var nameText = obj.transform.Find("Name")?.GetComponent<TMP_Text>();
            var carrotsText = obj.transform.Find("Carrots")?.GetComponent<TMP_Text>();
            var rankText = obj.transform.Find("Rank")?.GetComponent<TMP_Text>();

            if (nameText != null) nameText.text = entry.username;
            if (carrotsText != null) carrotsText.text = entry.carrots.ToString("N0");
            if (rankText != null) rankText.text = (i + 1).ToString();
        }
    }

    private void ClearList()
    {
        if (leaderboardListParent == null) return;

        for (int i = leaderboardListParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(leaderboardListParent.GetChild(i).gameObject);
            else DestroyImmediate(leaderboardListParent.GetChild(i).gameObject);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs) Debug.Log($"[LeaderboardManager] {message}");
    }

    public void ToggleSortOrder()
    {
        sortDescending = !sortDescending;
        UpdateSortButtonText();
        
        if (currentLeaderboard?.entries != null)
        {
            ApplySortAndDisplay();
            LogDebug($"Sort order toggled to {(sortDescending ? "descending" : "ascending")}");
        }
    }

    private void ApplySortAndDisplay()
    {
        if (currentLeaderboard?.entries == null) return;

        var sorted = sortDescending 
            ? currentLeaderboard.entries.OrderByDescending(e => e.carrots).ToArray()
            : currentLeaderboard.entries.OrderBy(e => e.carrots).ToArray();

        var sortedLeaderboard = new LeaderboardResponse { 
            message = currentLeaderboard.message, 
            entries = sorted 
        };

        currentLeaderboard = sortedLeaderboard;
        PopulateLeaderboard();
    }

    private void UpdateSortButtonText()
    {
        if (carrotsHeaderButton != null)
        {
            var buttonText = carrotsHeaderButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                string arrow = sortDescending ? "↓" : "↑";
                string baseText = buttonText.text.Replace("↓", "").Replace("↑", "").Trim();
                buttonText.text = $"{baseText} {arrow}";
            }
        }
    }

    public void OnBackButtonClicked()
    {
        LogDebug("Back button clicked, returning to game scene");
        SceneManager.LoadScene("game");
    }

    [ContextMenu("Refresh Leaderboard")]
    public void RefreshLeaderboard()
    {
        _ = LoadLeaderboardAsync();
    }
}

[Serializable]
public class LeaderboardResponse
{
    public string message;
    public LeaderboardUser[] entries;
}

[Serializable]
public class LeaderboardUser
{
    public int id;
    public string username;
    public int carrots;
    public int goldenCarrots;
}
