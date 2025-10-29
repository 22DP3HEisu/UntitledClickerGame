using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
    
    public List<PassiveClickerData> GetPassiveClickers() => passiveClickers;

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
    

    private void ProcessPassiveIncomeTick()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            GeneratePassiveIncome();
            tickTimer = 0f;
        }
    }

    private void GeneratePassiveIncome()
    {
        int totalIncome = CalculateTotalPassiveIncome();
        
        if (totalIncome > 0 && CurrencySyncManager.Instance != null)
        {
            CurrencySyncManager.Instance.AddCurrency(totalIncome);
        }
    }
    

    private int CalculateTotalPassiveIncome()
    {
        int totalIncome = 0;
        
        foreach (var clicker in passiveClickers)
        {
            int clickerIncome = CalculateClickerIncome(clicker);
            totalIncome += clickerIncome;
        }

        totalIncome = ApplyGlobalCPSBoost(totalIncome);
        
        return totalIncome;
    }

    private int CalculateClickerIncome(PassiveClickerData clicker)
    {
        float achievementBoost = GetAchievementBoost(clicker.name);
        float upgradePercentMultiplier = GetUpgradePercentMultiplier(clicker.name);

        float baseIncome = clicker.clicksPerSecond * clicker.level * tickInterval;
        float multipliedIncome = baseIncome * achievementBoost * upgradePercentMultiplier;
        
        float flatBonus = GetUpgradeFlatBonus(clicker.name) * clicker.level * tickInterval;
        
        return Mathf.RoundToInt(multipliedIncome + flatBonus);
    }

    #endregion

    #region Server Synchronization

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
    
    private bool IsValidClickerIndex(int index) => index >= 0 && index < passiveClickers.Count;

    private PassiveClickerData FindClickerByName(string buildingName)
    {
        foreach (var clicker in passiveClickers)
        {
            if (clicker.name.Equals(buildingName, StringComparison.OrdinalIgnoreCase))
                return clicker;
        }
        return null;
    }

    private float GetAchievementBoost(string buildingName)
    {
        return AchievementManager.Instance?.GetBuildingBoost(buildingName) ?? 1f;
    }

    private float GetUpgradePercentMultiplier(string buildingName)
    {
        return PassiveUpgradeManager.Instance?.GetBuildingPercentMultiplier(buildingName) ?? 1f;
    }

    private float GetUpgradeFlatBonus(string buildingName)
    {
        return PassiveUpgradeManager.Instance?.GetBuildingFlatClicksPerSecond(buildingName) ?? 0f;
    }
    
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