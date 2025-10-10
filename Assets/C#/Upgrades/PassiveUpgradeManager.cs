using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PassiveUpgradeManager : MonoBehaviour
{
    public static PassiveUpgradeManager Instance { get; private set; }

    // Event raised when an upgrade is purchased
    public event Action<PassiveUpgradeData> OnUpgradePurchased;

    [Header("Upgrade Definitions")]
    [SerializeField] private List<PassiveUpgradeData> upgrades = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Return all upgrades (for UI)
    public List<PassiveUpgradeData> GetAllUpgrades() => upgrades;

    // Purchase an upgrade by index (returns true on success)
    public bool PurchaseUpgrade(int index)
    {
        if (index < 0 || index >= upgrades.Count) return false;
        var up = upgrades[index];
        if (up == null) return false;
        if (up.isPurchased) return false;
        if (CurrencySyncManager.Instance == null) return false;

        if (!CurrencySyncManager.Instance.SpendCurrency(up.price)) return false;

        up.isPurchased = true;

        // Notify listeners (UI, etc.)
        try
        {
            OnUpgradePurchased?.Invoke(up);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PassiveUpgradeManager] Exception while invoking OnUpgradePurchased: {ex}");
        }

        // Optionally persist state here (PlayerPrefs / save system)
        return true;
    }

    // Purchase by id
    public bool PurchaseUpgradeById(string id)
    {
        var index = upgrades.FindIndex(u => u.id == id);
        if (index < 0) return false;
        return PurchaseUpgrade(index);
    }

    // Get cumulative percent multiplier for building (1.0 = no change)
    public float GetBuildingPercentMultiplier(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return 1f;
        float sumPercent = 0f;
        foreach (var u in upgrades)
        {
            if (!u.isPurchased) continue;
            if (u.targetBuildingName != buildingName) continue;
            if (u.rewardType == PassiveUpgradeData.RewardType.PercentBoost)
                sumPercent += u.rewardValue;
        }
        return 1f + (sumPercent / 100f);
    }

    // Get total flat clicks per second added to building from purchased upgrades
    public float GetBuildingFlatClicksPerSecond(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return 0f;
        float sum = 0f;
        foreach (var u in upgrades)
        {
            if (!u.isPurchased) continue;
            if (u.targetBuildingName != buildingName) continue;
            if (u.rewardType == PassiveUpgradeData.RewardType.FlatClicksPerSecond)
                sum += u.rewardValue;
        }
        return sum;
    }

    // Helper to check if an upgrade is already purchased
    public bool IsPurchased(string id)
    {
        var u = upgrades.FirstOrDefault(x => x.id == id);
        return u != null && u.isPurchased;
    }
}