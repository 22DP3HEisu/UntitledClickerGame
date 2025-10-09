using System;
using System.Collections.Generic;
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
}
