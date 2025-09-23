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
            totalClicks += Mathf.RoundToInt(clicker.clicksPerSecond * clicker.level * tickInterval);
        }
        if (totalClicks > 0)
            ClickManager.Instance.AddClicks(totalClicks);
    }

    // Upgrade a clicker by index
    public bool UpgradeClicker(int index, int currentClicks)
    {
        if (index >= 0 && index < passiveClickers.Count)
        {
            var clicker = passiveClickers[index];
            int price = clicker.GetCurrentPrice();
            if (currentClicks >= price)
            {
                ClickManager.Instance.AddClicks(-price); // Subtract price from clicks
                clicker.level++;
                return true; // Upgrade successful
            }
        }
        return false; // Not enough clicks or invalid index
    }

    public List<PassiveClickerData> GetPassiveClickers()
    {
        return passiveClickers;
    }
}