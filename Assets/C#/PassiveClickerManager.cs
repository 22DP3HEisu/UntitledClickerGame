using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Manages passive income generation from buildings/clickers with server synchronization
/// </summary>
public class PassiveClickerManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Passive Clicker Configuration")]
    [SerializeField] private List<PassiveClickerData> passiveClickers = new();
    
    [Header("Tick Settings")]
    [SerializeField] private float tickInterval = 1f;
    
    #endregion

    #region Private Fields
    
    private float tickTimer = 0f;
    
    #endregion

    #region Unity Lifecycle
    
    private void Update()
    {
        ProcessPassiveIncomeTick();
    }
    
    #endregion

    #region Public Interface
    
    /// <summary>
    /// Gets all passive clicker data for UI display
    /// </summary>
    /// <returns>List of passive clicker configurations</returns>
    public List<PassiveClickerData> GetPassiveClickers() => passiveClickers;
    
    /// <summary>
    /// Attempts to upgrade a clicker by spending currency
    /// </summary>
    /// <param name="index">Index of the clicker to upgrade</param>
    /// <param name="currentCarrots">Current currency amount (legacy parameter)</param>
    /// <returns>True if upgrade was successful, false otherwise</returns>
    public async Task<bool> UpgradeClicker(int index, int currentCarrots)
    {
        if (!IsValidClickerIndex(index) || CurrencySyncManager.Instance == null)
            return false;

        var clicker = passiveClickers[index];
        int price = clicker.GetCurrentPrice();
        
        if (!CurrencySyncManager.Instance.SpendCurrency(price))
            return false;

        clicker.level++;
        await SyncBuildingPurchaseAsync(clicker.name, clicker.level);
        return true;
    }
    
    /// <summary>
    /// Sets building level from server data (called during game startup)
    /// </summary>
    /// <param name="buildingName">Name of the building to update</param>
    /// <param name="level">New level from server</param>
    public void SetBuildingLevelFromServer(string buildingName, int level)
    {
        var clicker = FindClickerByName(buildingName);
        if (clicker != null)
        {
            clicker.level = level;
            Debug.Log($"[PassiveClickerManager] Set {buildingName} level to {level} from server");
        }
        else
        {
            Debug.LogWarning($"[PassiveClickerManager] Building '{buildingName}' not found in passive clickers list");
        }
    }
    
    #endregion

    #region Passive Income Processing
    
    /// <summary>
    /// Processes the passive income tick timer and generates income when ready
    /// </summary>
    private void ProcessPassiveIncomeTick()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            GeneratePassiveIncome();
            tickTimer = 0f;
        }
    }
    
    /// <summary>
    /// Calculates and adds passive income from all clickers
    /// </summary>
    private void GeneratePassiveIncome()
    {
        int totalIncome = CalculateTotalPassiveIncome();
        
        if (totalIncome > 0 && CurrencySyncManager.Instance != null)
        {
            CurrencySyncManager.Instance.AddCurrency(totalIncome);
        }
    }
    
    /// <summary>
    /// Calculates total passive income from all clickers for this tick
    /// </summary>
    /// <returns>Total income amount</returns>
    private int CalculateTotalPassiveIncome()
    {
        int totalIncome = 0;
        
        foreach (var clicker in passiveClickers)
        {
            int clickerIncome = CalculateClickerIncome(clicker);
            totalIncome += clickerIncome;
        }
        
        // Apply global CPS boost from achievements
        totalIncome = ApplyGlobalCPSBoost(totalIncome);
        
        return totalIncome;
    }
    
    /// <summary>
    /// Calculates income for a single clicker including all bonuses
    /// </summary>
    /// <param name="clicker">The clicker to calculate income for</param>
    /// <returns>Income amount for this clicker</returns>
    private int CalculateClickerIncome(PassiveClickerData clicker)
    {
        // Get all multipliers
        float achievementBoost = GetAchievementBoost(clicker.name);
        float upgradePercentMultiplier = GetUpgradePercentMultiplier(clicker.name);
        
        // Calculate base income with multipliers
        float baseIncome = clicker.clicksPerSecond * clicker.level * tickInterval;
        float multipliedIncome = baseIncome * achievementBoost * upgradePercentMultiplier;
        
        // Add flat bonuses from upgrades (per building level)
        float flatBonus = GetUpgradeFlatBonus(clicker.name) * clicker.level * tickInterval;
        
        return Mathf.RoundToInt(multipliedIncome + flatBonus);
    }
    
    #endregion

    #region Server Synchronization
    
    /// <summary>
    /// Syncs building purchase with server
    /// </summary>
    /// <param name="buildingName">Name of the building purchased</param>
    /// <param name="newLevel">New level after purchase</param>
    private async Task SyncBuildingPurchaseAsync(string buildingName, int newLevel)
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[PassiveClickerManager] Cannot sync building purchase - not logged in");
            return;
        }
        
        try
        {
            Debug.Log($"[PassiveClickerManager] Syncing building purchase: {buildingName} to level {newLevel}");
            
            var buildingData = new BuildingUpdateRequest { count = newLevel };
            var response = await ApiClient.PostAsync<BuildingUpdateRequest, BuildingResponse>($"/user/building/{buildingName}", buildingData);
            
            if (response != null)
            {
                Debug.Log($"[PassiveClickerManager] Building purchase synced successfully: {buildingName} level {newLevel}");
            }
        }
        catch (ApiException ex)
        {
            Debug.LogWarning($"[PassiveClickerManager] Failed to sync building purchase with server: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PassiveClickerManager] Error syncing building purchase: {ex.Message}");
        }
    }
    
    #endregion

    #region Private Helpers
    
    /// <summary>
    /// Validates if the clicker index is within valid range
    /// </summary>
    /// <param name="index">Index to validate</param>
    /// <returns>True if index is valid</returns>
    private bool IsValidClickerIndex(int index) => index >= 0 && index < passiveClickers.Count;
    
    /// <summary>
    /// Finds a clicker by name (case-insensitive)
    /// </summary>
    /// <param name="buildingName">Name of the building to find</param>
    /// <returns>Matching clicker or null if not found</returns>
    private PassiveClickerData FindClickerByName(string buildingName)
    {
        foreach (var clicker in passiveClickers)
        {
            if (clicker.name.Equals(buildingName, StringComparison.OrdinalIgnoreCase))
                return clicker;
        }
        return null;
    }
    
    /// <summary>
    /// Gets achievement boost multiplier for a building
    /// </summary>
    /// <param name="buildingName">Name of the building</param>
    /// <returns>Achievement boost multiplier (1.0+ means boost)</returns>
    private float GetAchievementBoost(string buildingName)
    {
        return AchievementManager.Instance?.GetBuildingBoost(buildingName) ?? 1f;
    }
    
    /// <summary>
    /// Gets upgrade percent multiplier for a building
    /// </summary>
    /// <param name="buildingName">Name of the building</param>
    /// <returns>Upgrade percent multiplier (1.0+ means boost)</returns>
    private float GetUpgradePercentMultiplier(string buildingName)
    {
        return PassiveUpgradeManager.Instance?.GetBuildingPercentMultiplier(buildingName) ?? 1f;
    }
    
    /// <summary>
    /// Gets flat upgrade bonus per second for a building
    /// </summary>
    /// <param name="buildingName">Name of the building</param>
    /// <returns>Flat bonus clicks per second</returns>
    private float GetUpgradeFlatBonus(string buildingName)
    {
        return PassiveUpgradeManager.Instance?.GetBuildingFlatClicksPerSecond(buildingName) ?? 0f;
    }
    
    /// <summary>
    /// Applies global CPS boost from achievements
    /// </summary>
    /// <param name="totalIncome">Base total income</param>
    /// <returns>Income with global CPS boost applied</returns>
    private int ApplyGlobalCPSBoost(int totalIncome)
    {
        if (AchievementManager.Instance != null)
        {
            float cpsBoost = AchievementManager.Instance.GetCPSBoost();
            return Mathf.RoundToInt(totalIncome * cpsBoost);
        }
        return totalIncome;
    }
    
    #endregion
}

// Data structures for building API requests and responses
[System.Serializable]
public class BuildingUpdateRequest
{
    public int count;
}

[System.Serializable]
public class BuildingResponse
{
    public string message;
    public BuildingInfo building;
}

[System.Serializable]
public class BuildingInfo
{
    public string name;
    public int count;
}