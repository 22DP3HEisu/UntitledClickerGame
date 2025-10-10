using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class ShopUIManager : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemPrefab;

    private PassiveClickerManager clickerManager;

    void Start()
    {
        // Hide or deactivate the template if it exists in the scene
        var template = itemListParent.Find("ShopItemTemplate");
        if (template != null)
        {
            template.gameObject.SetActive(false);
        }

        clickerManager = FindFirstObjectByType<PassiveClickerManager>();
        GenerateShopItems();
    }

    private void GenerateShopItems()
    {
        var clickers = clickerManager.GetPassiveClickers();
        for (int i = 0; i < clickers.Count; i++)
        {
            var itemGO = Instantiate(itemPrefab, itemListParent);
            itemGO.SetActive(true); // Ensure the clone is active
            var itemUI = itemGO.GetComponent<PassiveClickerShopItemUI>();
            itemUI.Setup(clickers[i], i, this);
        }
        RefreshShopItems();
    }

    public void RefreshShopItems()
    {
        var clickers = clickerManager.GetPassiveClickers();
        for (int i = 0; i < itemListParent.childCount; i++)
        {
            var itemUI = itemListParent.GetChild(i).GetComponent<PassiveClickerShopItemUI>();

            if (i == 0)
            {
                itemUI.SetInteractable(true);
                itemUI.SetVisible(true);
            }
            else if (i < clickers.Count)
            {
                bool show = clickers[i - 1].level >= 1;
                itemUI.SetVisible(show);
                itemUI.SetInteractable(show);
            }
            else
            {
                itemUI.SetVisible(false);
            }
        }
    }

    public async void OnBuyClicker(int index)
    {
        var clicker = clickerManager.GetPassiveClickers()[index];
        int currentCarrots = CurrencySyncManager.Instance != null ? CurrencySyncManager.Instance.Carrots : 0;
        
        if (clickerManager.UpgradeClicker(index, currentCarrots))
        {
            // Update UI immediately
            var itemUI = itemListParent.GetChild(index).GetComponent<PassiveClickerShopItemUI>();
            itemUI.Setup(clicker, index, this);
            RefreshShopItems();
            
            // Send upgrade purchase to server
            await PurchaseUpgradeOnServer(clicker.name, index);
        }
    }
    
    private async Task PurchaseUpgradeOnServer(string upgradeeName, int level)
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[ShopUI] Cannot sync upgrade purchase - not logged in");
            return;
        }
        
        try
        {
            // Create upgrade name with level for tracking
            string upgradeName = $"{upgradeeName}_Level_{level}";
            
            Debug.Log($"[ShopUI] Purchasing upgrade on server: {upgradeName}");
            
            var response = await ApiClient.PostAsync<object, UpgradeResponse>($"/user/upgrade/{upgradeName}", null);
            
            if (response != null)
            {
                Debug.Log($"[ShopUI] Upgrade purchase successful: {upgradeName}");
            }
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == 400 && ex.Message.Contains("already owned"))
            {
                // This is expected if the user already has this upgrade level
                Debug.Log($"[ShopUI] Upgrade already owned on server: {upgradeeName}_Level_{level}");
            }
            else
            {
                Debug.LogWarning($"[ShopUI] Failed to purchase upgrade on server: {ex.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopUI] Error purchasing upgrade on server: {ex.Message}");
        }
    }
}

// Data structure for upgrade API response
[System.Serializable]
public class UpgradeResponse
{
    public string message;
    public UpgradeInfo upgrade;
}

[System.Serializable]
public class UpgradeInfo
{
    public string name;
    public string purchasedAt;
}