using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Handles passive upgrade purchases, tracking, and server synchronization
/// </summary>
public class PassiveUpgradeManager : MonoBehaviour
{
    #region Singleton & Events
    
    public static PassiveUpgradeManager Instance { get; private set; }
    
    /// <summary>
    /// Event raised when an upgrade is purchased
    /// </summary>
    public event Action<PassiveUpgradeData> OnUpgradePurchased;
    
    #endregion

    #region Serialized Fields
    
    [Header("Upgrade Definitions")]
    [SerializeField] private List<PassiveUpgradeData> upgrades = new();
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeSingleton();
    }
    
    #endregion

    #region Initialization
    
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #endregion

    #region Public Interface
    
    /// <summary>
    /// Returns all available upgrades for UI display
    /// </summary>
    /// <returns>List of all upgrade definitions</returns>
    public List<PassiveUpgradeData> GetAllUpgrades() => upgrades;

    /// <summary>
    /// Attempts to purchase an upgrade by its index
    /// </summary>
    /// <param name="index">Index of the upgrade in the upgrades list</param>
    /// <returns>True if purchase was successful, false otherwise</returns>
    public bool PurchaseUpgrade(int index)
    {
        if (index < 0 || index >= upgrades.Count) return false;
        var up = upgrades[index];
        if (up == null) return false;
        if (up.isPurchased) return false;
        if (CurrencySyncManager.Instance == null) return false;

        if (!CurrencySyncManager.Instance.SpendCurrency(up.price)) return false;

        up.isPurchased = true;

        // Sync upgrade purchase with server
        _ = SyncUpgradePurchaseAsync(up.upgradeName);

        // Notify listeners (UI, etc.)
        InvokeUpgradePurchased(up);

        // Optionally persist state here (PlayerPrefs / save system)
        return true;
    }

    /// <summary>
    /// Attempts to purchase an upgrade by its unique ID
    /// </summary>
    /// <param name="id">Unique identifier of the upgrade</param>
    /// <returns>True if purchase was successful, false otherwise</returns>
    public bool PurchaseUpgradeById(string id)
    {
        var index = upgrades.FindIndex(u => u.id == id);
        if (index < 0) return false;
        return PurchaseUpgrade(index);
    }

    /// <summary>
    /// Gets the cumulative percent multiplier for a specific building from purchased upgrades
    /// </summary>
    /// <param name="buildingName">Name of the building to check</param>
    /// <returns>Multiplier value (1.0 = no change, 1.5 = 50% increase)</returns>
    public float GetBuildingPercentMultiplier(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return 1f;
        
        var percentBoost = upgrades
            .Where(u => u.isPurchased && 
                       u.targetBuildingName == buildingName && 
                       u.rewardType == PassiveUpgradeData.RewardType.PercentBoost)
            .Sum(u => u.rewardValue);
            
        return 1f + (percentBoost / 100f);
    }

    /// <summary>
    /// Gets the total flat clicks per second added to a building from purchased upgrades
    /// </summary>
    /// <param name="buildingName">Name of the building to check</param>
    /// <returns>Total flat clicks per second bonus</returns>
    public float GetBuildingFlatClicksPerSecond(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return 0f;
        
        return upgrades
            .Where(u => u.isPurchased && 
                       u.targetBuildingName == buildingName && 
                       u.rewardType == PassiveUpgradeData.RewardType.FlatClicksPerSecond)
            .Sum(u => u.rewardValue);
    }

    /// <summary>
    /// Checks if an upgrade has already been purchased
    /// </summary>
    /// <param name="id">Unique identifier of the upgrade</param>
    /// <returns>True if the upgrade is purchased, false otherwise</returns>
    public bool IsPurchased(string id)
    {
        var upgrade = upgrades.FirstOrDefault(x => x.id == id);
        return upgrade != null && upgrade.isPurchased;
    }
    
    /// <summary>
    /// Applies upgrade from server data (called during game startup)
    /// </summary>
    /// <param name="serverUpgradeName">Name of the upgrade from server</param>
    public void ApplyUpgradeFromServer(string serverUpgradeName)
    {
        var upgrade = FindUpgradeByServerName(serverUpgradeName);
        
        if (upgrade != null)
        {
            ApplyServerUpgrade(upgrade, serverUpgradeName);
        }
        else
        {
            Debug.LogWarning($"[PassiveUpgradeManager] Upgrade '{serverUpgradeName}' not found in local upgrades list");
        }
    }
    
    #endregion

    #region Server Sync
    
    /// <summary>
    /// Syncs upgrade purchase with server
    /// </summary>
    /// <param name="upgradeName">Name of the upgrade to sync</param>
    private async Task SyncUpgradePurchaseAsync(string upgradeName)
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[PassiveUpgradeManager] Cannot sync upgrade purchase - not logged in");
            return;
        }
        
        try
        {
            Debug.Log($"[PassiveUpgradeManager] Syncing upgrade purchase with server: {upgradeName}");
            
            var response = await ApiClient.PostAsync<object, UpgradePurchaseResponse>($"/user/upgrade/{upgradeName}", null);
            
            if (response != null)
            {
                Debug.Log($"[PassiveUpgradeManager] Upgrade purchase synced successfully: {upgradeName}");
            }
        }
        catch (ApiException ex)
        {
            Debug.LogError($"[PassiveUpgradeManager] Failed to sync upgrade purchase with server: {ex.Message}");
            
            // If server sync fails, we could optionally revert the local state
            // For now, we'll let the local change persist and retry on next sync
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PassiveUpgradeManager] Upgrade sync error: {ex.Message}");
        }
    }
    
    #endregion

    #region Private Helpers
    
    /// <summary>
    /// Finds an upgrade by server name using various matching strategies
    /// </summary>
    /// <param name="serverUpgradeName">Name from server</param>
    /// <returns>Matching upgrade or null if not found</returns>
    private PassiveUpgradeData FindUpgradeByServerName(string serverUpgradeName)
    {
        // Try exact upgradeName match first
        var upgrade = upgrades.FirstOrDefault(u => 
            u.upgradeName.Equals(serverUpgradeName, StringComparison.OrdinalIgnoreCase));
        
        // If not found by upgradeName, try by ID
        if (upgrade == null)
        {
            upgrade = upgrades.FirstOrDefault(u => 
                u.id.Equals(serverUpgradeName, StringComparison.OrdinalIgnoreCase));
        }
        
        // If still not found, try level-based upgrade names (e.g., "AutoClicker_Level_5")
        if (upgrade == null && serverUpgradeName.Contains("_Level_"))
        {
            string baseName = serverUpgradeName.Substring(0, serverUpgradeName.LastIndexOf("_Level_"));
            upgrade = upgrades.FirstOrDefault(u => 
                u.upgradeName.Equals(baseName, StringComparison.OrdinalIgnoreCase) ||
                u.id.Equals(baseName, StringComparison.OrdinalIgnoreCase));
        }
        
        return upgrade;
    }
    
    /// <summary>
    /// Applies a server upgrade to the local state
    /// </summary>
    /// <param name="upgrade">The upgrade to apply</param>
    /// <param name="serverUpgradeName">Original server name for logging</param>
    private void ApplyServerUpgrade(PassiveUpgradeData upgrade, string serverUpgradeName)
    {
        if (!upgrade.isPurchased)
        {
            upgrade.isPurchased = true;
            Debug.Log($"[PassiveUpgradeManager] Applied upgrade from server: {serverUpgradeName} -> {upgrade.upgradeName}");
            
            // Notify listeners (but don't spend currency since it's from server)
            InvokeUpgradePurchased(upgrade);
        }
        else
        {
            Debug.Log($"[PassiveUpgradeManager] Upgrade '{serverUpgradeName}' already purchased locally");
        }
    }
    
    /// <summary>
    /// Safely invokes the OnUpgradePurchased event
    /// </summary>
    /// <param name="upgrade">The upgrade that was purchased</param>
    private void InvokeUpgradePurchased(PassiveUpgradeData upgrade)
    {
        try
        {
            OnUpgradePurchased?.Invoke(upgrade);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PassiveUpgradeManager] Exception while invoking OnUpgradePurchased: {ex}");
        }
    }
    
    #endregion
}

// Data structures for upgrade purchase sync
[Serializable]
public class UpgradePurchaseResponse
{
    public string message;
    public UpgradePurchaseData upgrade;
}

[Serializable]
public class UpgradePurchaseData
{
    public string name;
    public string purchasedAt;
}