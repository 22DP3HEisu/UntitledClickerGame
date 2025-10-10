using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Achievement Types
public enum AchievementType
{
    TotalClicks,
    TotalCarrots,
    CarrotsPerSecond,
    TotalBuildingCount,
    SingleBuildingCount,
    FinishGame,
    DailyStreak,
    RandomQuests,
    JoinClan,
    DonateCarrot
}

// Reward Types
public enum RewardType
{
    BoostAllPercent,
    BoostAllFlat,
    BoostSinglePercent,
    BoostSingleFlat,
    Unlock,
    TemporaryBoost
}

// Target for single boost
public enum BoostTarget
{
    ClickIncome,
    Building,
    CPS,
    CarrotGain,
    QuestReward,
    ClanBonus
}

// Self-contained achievement class (renamed to avoid Unity type collision)
[Serializable]
public class AchievementItem
{
    [Header("Achievement Info")]
    public string id;
    public string achievementName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Requirements")]
    public AchievementType type;
    public int targetValue;
    [Tooltip("Only needed for SingleBuildingCount type - must match PassiveClickerData name")]
    public string targetBuildingName;

    [Header("Reward")]
    public RewardType rewardType;
    [Tooltip("Only needed for single boost rewards")]
    public BoostTarget boostTarget;
    [Tooltip("Percentage (e.g., 10 = +10%) or flat value")]
    public float rewardValue;
    [Tooltip("Only for TemporaryBoost type - duration in seconds")]
    public float temporaryDuration;

    [Header("Runtime State (Read Only)")]
    [SerializeField] private bool isCompleted;
    [SerializeField] private int currentProgress;

    // Properties for external access
    public bool IsCompleted => isCompleted;
    public int CurrentProgress => currentProgress;

    public void SetProgress(int progress)
    {
        currentProgress = progress;
    }

    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
    }

    public float GetProgressPercentage()
    {
        if (targetValue <= 0) return 0f;
        return Mathf.Clamp01((float)currentProgress / targetValue);
    }
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievement Definitions")]
    [Tooltip("Create your achievements here by expanding the list")]
    [SerializeField] private List<AchievementItem> achievements = new List<AchievementItem>();

    // Boost multipliers
    private Dictionary<string, float> buildingBoosts = new Dictionary<string, float>();
    private float globalProductionBoost = 1f;
    private float clickIncomeBoost = 1f;
    private float cpsBoost = 1f;
    private float carrotGainBoost = 1f;

    // Temporary boosts
    private List<TemporaryBoost> activeTemporaryBoosts = new List<TemporaryBoost>();

    // Stats tracking
    private int totalClicks = 0;
    private int dailyStreak = 0;
    private int randomQuestsCompleted = 0;
    private bool hasJoinedClan = false;
    private int totalCarrotsDonated = 0;

    // Cached reference to avoid repeated lookups
    private PassiveClickerManager cachedPassiveManager;

    public event Action<AchievementItem> OnAchievementCompleted;

    [Serializable]
    private class TemporaryBoost
    {
        public AchievementItem achievement;
        public float endTime;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Cache the PassiveClickerManager reference
        cachedPassiveManager = FindFirstObjectByType<PassiveClickerManager>();
    }

    async void Start()
    {
        // Subscribe to game data loaded event for server sync
        CurrencySyncManager.OnGameDataLoaded += HandleGameDataLoaded;
        
        // Load achievements from server on startup
        await LoadAchievementsFromServerAsync();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        CurrencySyncManager.OnGameDataLoaded -= HandleGameDataLoaded;
    }

    private void HandleGameDataLoaded()
    {
        // Reload achievements when game data is loaded from server
        Debug.Log("[AchievementManager] Game data loaded - refreshing achievements from server");
        _ = LoadAchievementsFromServerAsync();
    }

    void Update()
    {
        // Update temporary boosts
        for (int i = activeTemporaryBoosts.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeTemporaryBoosts[i].endTime)
            {
                RemoveTemporaryBoost(activeTemporaryBoosts[i]);
                activeTemporaryBoosts.RemoveAt(i);
            }
        }

        // Update achievement progress
        UpdateAchievementProgress();
    }
    
    private void UpdateAchievementProgress()
    {
        foreach (var achievement in achievements)
        {
            if (achievement.IsCompleted) continue;

            int currentValue = 0;

            switch (achievement.type)
            {
                case AchievementType.TotalClicks:
                    currentValue = totalClicks;
                    break;

                case AchievementType.TotalCarrots:
                    currentValue = CurrencySyncManager.Instance != null ? CurrencySyncManager.Instance.Carrots : 0;
                    break;

                case AchievementType.CarrotsPerSecond:
                    currentValue = Mathf.RoundToInt(GetCurrentCPS());
                    break;

                case AchievementType.TotalBuildingCount:
                    currentValue = GetTotalBuildingCount();
                    break;

                case AchievementType.SingleBuildingCount:
                    currentValue = GetSingleBuildingCount(achievement.targetBuildingName);
                    break;

                case AchievementType.DailyStreak:
                    currentValue = dailyStreak;
                    break;

                case AchievementType.RandomQuests:
                    currentValue = randomQuestsCompleted;
                    break;

                case AchievementType.JoinClan:
                    currentValue = hasJoinedClan ? 1 : 0;
                    break;

                case AchievementType.DonateCarrot:
                    currentValue = totalCarrotsDonated;
                    break;
            }

            achievement.SetProgress(currentValue);

            if (currentValue >= achievement.targetValue)
            {
                CompleteAchievement(achievement);
            }
        }
    }

    private void CompleteAchievement(AchievementItem achievement)
    {
        achievement.SetCompleted(true);
        ApplyReward(achievement);
        OnAchievementCompleted?.Invoke(achievement);
        Debug.Log($"Achievement Completed: {achievement.achievementName} - Reward: {achievement.rewardValue}");
        
        // Sync achievement completion with server
        _ = SaveAchievementToServerAsync(achievement);
    }

    private void ApplyReward(AchievementItem achievement)
    {
        switch (achievement.rewardType)
        {
            case RewardType.BoostAllPercent:
                globalProductionBoost += achievement.rewardValue / 100f;
                break;

            case RewardType.BoostAllFlat:
                // Applied in calculation methods
                break;

            case RewardType.BoostSinglePercent:
                ApplySingleBoostPercent(achievement);
                break;

            case RewardType.BoostSingleFlat:
                ApplySingleBoostFlat(achievement);
                break;

            case RewardType.TemporaryBoost:
                ApplyTemporaryBoost(achievement);
                break;

            case RewardType.Unlock:
                // Handle unlocks (cosmetics, features, etc.)
                break;
        }
    }

    private void ApplySingleBoostPercent(AchievementItem achievement)
    {
        switch (achievement.boostTarget)
        {
            case BoostTarget.ClickIncome:
                clickIncomeBoost += achievement.rewardValue / 100f;
                break;

            case BoostTarget.CPS:
                cpsBoost += achievement.rewardValue / 100f;
                break;

            case BoostTarget.CarrotGain:
                carrotGainBoost += achievement.rewardValue / 100f;
                break;

            case BoostTarget.Building:
                if (!string.IsNullOrEmpty(achievement.targetBuildingName))
                {
                    if (!buildingBoosts.ContainsKey(achievement.targetBuildingName))
                        buildingBoosts[achievement.targetBuildingName] = 1f;
                    buildingBoosts[achievement.targetBuildingName] += achievement.rewardValue / 100f;
                }
                break;
        }
    }

    private void ApplySingleBoostFlat(AchievementItem achievement)
    {
        // Flat boosts are handled in the calculation methods
    }

    private void ApplyTemporaryBoost(AchievementItem achievement)
    {
        var tempBoost = new TemporaryBoost
        {
            achievement = achievement,
            endTime = Time.time + achievement.temporaryDuration
        };
        activeTemporaryBoosts.Add(tempBoost);
    }

    private void RemoveTemporaryBoost(TemporaryBoost boost)
    {
        // Reverse the boost effect
        switch (boost.achievement.rewardType)
        {
            case RewardType.TemporaryBoost:
                // Remove temporary effects
                break;
        }
    }

    // Public methods to track player actions
    public void OnPlayerClick()
    {
        totalClicks++;
        Debug.Log("OnPlayerClick called: " + totalClicks);
    }

    public void OnDailyCompleted()
    {
        dailyStreak++;
    }

    public void OnRandomQuestCompleted()
    {
        randomQuestsCompleted++;
    }

    public void OnClanJoined()
    {
        hasJoinedClan = true;
    }

    public void OnCarrotDonated(int amount)
    {
        totalCarrotsDonated += amount;
    }

    public void OnGameFinished()
    {
        // Trigger finish game achievements
    }

    // Boost getters for other systems to use
    public float GetBuildingBoost(string buildingName)
    {
        float boost = globalProductionBoost;

        if (buildingBoosts.ContainsKey(buildingName))
            boost *= buildingBoosts[buildingName];

        return boost;
    }

    public float GetClickIncomeBoost()
    {
        return clickIncomeBoost * globalProductionBoost;
    }

    public float GetCPSBoost()
    {
        return cpsBoost * globalProductionBoost;
    }

    public float GetCarrotGainBoost()
    {
        return carrotGainBoost * globalProductionBoost;
    }

    public float GetGlobalBoost()
    {
        return globalProductionBoost;
    }

    // Helper methods
    private int GetTotalBuildingCount()
    {
        if (cachedPassiveManager == null) return 0;

        int total = 0;
        foreach (var clicker in cachedPassiveManager.GetPassiveClickers())
        {
            total += clicker.level;
        }
        return total;
    }

    private int GetSingleBuildingCount(string buildingName)
    {
        if (cachedPassiveManager == null) return 0;

        foreach (var clicker in cachedPassiveManager.GetPassiveClickers())
        {
            if (clicker.name == buildingName)
                return clicker.level;
        }
        return 0;
    }

    private float GetCurrentCPS()
    {
        if (cachedPassiveManager == null) return 0;

        float total = 0;
        foreach (var clicker in cachedPassiveManager.GetPassiveClickers())
        {
            total += clicker.clicksPerSecond * clicker.level * GetBuildingBoost(clicker.name);
        }
        return total * GetCPSBoost();
    }

    // Achievement list getters
    public List<AchievementItem> GetAllAchievements()
    {
        return achievements;
    }

    public List<AchievementItem> GetCompletedAchievements()
    {
        return achievements.FindAll(a => a.IsCompleted);
    }

    public List<AchievementItem> GetIncompleteAchievements()
    {
        return achievements.FindAll(a => !a.IsCompleted);
    }

    #region Server Synchronization

    /// <summary>
    /// Loads completed achievements from server and applies them locally
    /// </summary>
    private async Task LoadAchievementsFromServerAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[AchievementManager] Cannot load achievements - not logged in");
            return;
        }

        try
        {
            Debug.Log("[AchievementManager] Loading achievements from server...");

            var response = await ApiClient.GetAsync<AchievementListResponse>("/user/achievements");

            if (response?.achievements != null)
            {
                ApplyServerAchievements(response.achievements);
                Debug.Log($"[AchievementManager] Loaded {response.achievements.Count} achievements from server");
            }
        }
        catch (ApiException ex)
        {
            Debug.LogWarning($"[AchievementManager] Failed to load achievements from server: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AchievementManager] Error loading achievements from server: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves a completed achievement to the server
    /// </summary>
    /// <param name="achievement">The completed achievement to save</param>
    private async Task SaveAchievementToServerAsync(AchievementItem achievement)
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[AchievementManager] Cannot save achievement - not logged in");
            return;
        }

        try
        {
            Debug.Log($"[AchievementManager] Saving achievement to server: {achievement.id}");

            var achievementData = new AchievementCompletionRequest
            {
                achievementId = achievement.id,
                completedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var response = await ApiClient.PostAsync<AchievementCompletionRequest, AchievementCompletionResponse>(
                "/user/achievements", achievementData);

            if (response != null)
            {
                Debug.Log($"[AchievementManager] Achievement saved successfully: {achievement.id}");
            }
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == 400 && ex.Message.Contains("already completed"))
            {
                Debug.Log($"[AchievementManager] Achievement already completed on server: {achievement.id}");
            }
            else
            {
                Debug.LogWarning($"[AchievementManager] Failed to save achievement to server: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AchievementManager] Error saving achievement to server: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies achievements from server data to local achievements
    /// </summary>
    /// <param name="serverAchievements">List of completed achievements from server</param>
    private void ApplyServerAchievements(List<ServerAchievementData> serverAchievements)
    {
        foreach (var serverAchievement in serverAchievements)
        {
            var localAchievement = achievements.Find(a => a.id.Equals(serverAchievement.achievementId, StringComparison.OrdinalIgnoreCase));
            
            if (localAchievement != null && !localAchievement.IsCompleted)
            {
                Debug.Log($"[AchievementManager] Applying server achievement: {localAchievement.achievementName}");
                
                // Set as completed without triggering server sync again
                localAchievement.SetCompleted(true);
                localAchievement.SetProgress(localAchievement.targetValue);
                
                // Apply the reward
                ApplyReward(localAchievement);
                
                // Notify listeners (but don't save to server again)
                OnAchievementCompleted?.Invoke(localAchievement);
            }
            else if (localAchievement == null)
            {
                Debug.LogWarning($"[AchievementManager] Server achievement '{serverAchievement.achievementId}' not found in local achievements");
            }
        }
    }

    #endregion
}

// Data structures for achievement API requests and responses
[Serializable]
public class AchievementListResponse
{
    public List<ServerAchievementData> achievements;
}

[Serializable]
public class ServerAchievementData
{
    public string achievementId;
    public string completedAt;
}

[Serializable]
public class AchievementCompletionRequest
{
    public string achievementId;
    public string completedAt;
}

[Serializable]
public class AchievementCompletionResponse
{
    public string message;
    public ServerAchievementData achievement;
}
