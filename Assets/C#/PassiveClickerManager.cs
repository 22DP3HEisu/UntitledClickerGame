using System.Collections.Generic;
using UnityEngine;

public class PassiveClickerManager : MonoBehaviour
{
    [SerializeField]
    private List<PassiveClickerData> passiveClickers = new();

    [SerializeField]
    private float tickInterval = 1f;
    private float tickTimer = 0f;

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            AddPassiveClicks();
            tickTimer = 0f;
        }
    }

    private void AddPassiveClicks()
    {
        int totalClicks = 0;
        foreach (var clicker in passiveClickers)
        {
            // Apply achievement boosts
            float achievementBoost = AchievementManager.Instance != null
                ? AchievementManager.Instance.GetBuildingBoost(clicker.name)
                : 1f;

            // Apply upgrade percent multiplier (from one-time upgrades)
            float upgradePercentMultiplier = PassiveUpgradeManager.Instance != null
                ? PassiveUpgradeManager.Instance.GetBuildingPercentMultiplier(clicker.name)
                : 1f;

            // Calculate base clicks from clicker (levels still supported for clickers)
            float baseClicks = clicker.clicksPerSecond * clicker.level * tickInterval;

            // Apply multipliers
            float multiplied = baseClicks * achievementBoost * upgradePercentMultiplier;

            // Add flat clicks/sec from upgrades (converted for this tick interval)
            // IMPORTANT: flat upgrades are applied per generator instance, so multiply by level
            float flatClicksPerSec = PassiveUpgradeManager.Instance != null
                ? PassiveUpgradeManager.Instance.GetBuildingFlatClicksPerSecond(clicker.name)
                : 0f;
            float flatForTick = flatClicksPerSec * clicker.level * tickInterval;

            int clicks = Mathf.RoundToInt(multiplied + flatForTick);
            totalClicks += clicks;
        }

        // Apply CPS boost
        if (AchievementManager.Instance != null)
        {
            totalClicks = Mathf.RoundToInt(totalClicks * AchievementManager.Instance.GetCPSBoost());
        }

        if (totalClicks > 0 && CurrencySyncManager.Instance != null)
            CurrencySyncManager.Instance.AddCurrency(totalClicks);
    }

    // Upgrade a clicker by index (keeps existing behavior)
    public bool UpgradeClicker(int index, int currentCarrots)
    {
        if (index >= 0 && index < passiveClickers.Count && CurrencySyncManager.Instance != null)
        {
            var clicker = passiveClickers[index];
            int price = clicker.GetCurrentPrice();
            if (CurrencySyncManager.Instance.SpendCurrency(price))
            {
                clicker.level++;
                return true;
            }
        }
        return false;
    }

    public List<PassiveClickerData> GetPassiveClickers()
    {
        return passiveClickers;
    }
}