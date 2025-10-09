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
            float boost = AchievementManager.Instance != null
                ? AchievementManager.Instance.GetBuildingBoost(clicker.name)
                : 1f;

            int clicks = Mathf.RoundToInt(clicker.clicksPerSecond * clicker.level * tickInterval * boost);
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

    // Upgrade a clicker by index
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