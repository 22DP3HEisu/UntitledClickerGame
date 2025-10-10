using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the shop UI for purchasing passive clicker buildings/upgrades
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI References")]
    [Tooltip("Main shop panel GameObject")]
    [SerializeField] private GameObject shopPanel;
    
    [Tooltip("Parent transform for shop item instances")]
    [SerializeField] private Transform itemListParent;
    
    [Tooltip("Prefab for individual shop items")]
    [SerializeField] private GameObject itemPrefab;
    
    #endregion

    #region Private Fields
    
    private PassiveClickerManager clickerManager;
    
    #endregion

    #region Unity Lifecycle
    
    private void Start()
    {
        InitializeShop();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initializes the shop UI and sets up necessary components
    /// </summary>
    private void InitializeShop()
    {
        HideTemplateItems();
        InitializeClickerManager();
        SubscribeToEvents();
        GenerateShopItems();
    }
    
    /// <summary>
    /// Hides any template items that may exist in the scene
    /// </summary>
    private void HideTemplateItems()
    {
        var template = itemListParent?.Find("ShopItemTemplate");
        if (template != null)
        {
            template.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Initializes the passive clicker manager reference
    /// </summary>
    private void InitializeClickerManager()
    {
        clickerManager = FindFirstObjectByType<PassiveClickerManager>();
        
        if (clickerManager == null)
        {
            Debug.LogWarning("[ShopUIManager] PassiveClickerManager not found in scene!");
        }
    }
    
    /// <summary>
    /// Subscribes to necessary events
    /// </summary>
    private void SubscribeToEvents()
    {
        CurrencySyncManager.OnGameDataLoaded += HandleGameDataLoaded;
    }
    
    /// <summary>
    /// Unsubscribes from events to prevent memory leaks
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        CurrencySyncManager.OnGameDataLoaded -= HandleGameDataLoaded;
    }
    
    #endregion

    #region Event Handlers
    
    /// <summary>
    /// Handles game data loaded events by refreshing the shop display
    /// </summary>
    private void HandleGameDataLoaded()
    {
        Debug.Log("[ShopUIManager] Game data loaded from server - refreshing shop display");
        RefreshShopItems();
    }
    
    #endregion

    #region Public Interface
    
    /// <summary>
    /// Refreshes all shop items to reflect current game state
    /// </summary>
    public void RefreshShopItems()
    {
        if (!CanRefreshShopItems())
            return;

        var clickers = clickerManager.GetPassiveClickers();
        UpdateShopItemsDisplay(clickers);
    }

    /// <summary>
    /// Handles the purchase of a clicker/building by index
    /// </summary>
    /// <param name="index">Index of the clicker to purchase</param>
    public async void OnBuyClicker(int index)
    {
        if (!CanPurchaseClicker(index))
            return;

        var clicker = clickerManager.GetPassiveClickers()[index];
        int currentCarrots = GetCurrentCurrency();
        
        bool upgradeSuccessful = await AttemptClickerUpgrade(index, currentCarrots);
        
        if (upgradeSuccessful)
        {
            await HandleSuccessfulPurchase(clicker, index);
        }
    }
    
    #endregion

    #region Shop Management
    
    /// <summary>
    /// Generates all shop items based on available clickers
    /// </summary>
    private void GenerateShopItems()
    {
        if (!CanGenerateShopItems())
            return;

        var clickers = clickerManager.GetPassiveClickers();
        CreateShopItemInstances(clickers);
        RefreshShopItems();
    }
    
    /// <summary>
    /// Creates shop item instances for each clicker
    /// </summary>
    /// <param name="clickers">List of clicker data</param>
    private void CreateShopItemInstances(System.Collections.Generic.List<PassiveClickerData> clickers)
    {
        for (int i = 0; i < clickers.Count; i++)
        {
            CreateSingleShopItem(clickers[i], i);
        }
    }
    
    /// <summary>
    /// Creates a single shop item instance
    /// </summary>
    /// <param name="clickerData">Data for the clicker</param>
    /// <param name="index">Index of the clicker</param>
    private void CreateSingleShopItem(PassiveClickerData clickerData, int index)
    {
        var itemObject = Instantiate(itemPrefab, itemListParent);
        itemObject.SetActive(true);
        
        var itemUI = itemObject.GetComponent<PassiveClickerShopItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(clickerData, index, this);
        }
        else
        {
            Debug.LogWarning($"[ShopUIManager] Shop item prefab missing PassiveClickerShopItemUI component at index {index}");
        }
    }
    
    /// <summary>
    /// Updates the display of all shop items
    /// </summary>
    /// <param name="clickers">Current clicker data</param>
    private void UpdateShopItemsDisplay(System.Collections.Generic.List<PassiveClickerData> clickers)
    {
        for (int i = 0; i < itemListParent.childCount; i++)
        {
            var itemUI = GetShopItemUI(i);
            if (itemUI == null) continue;

            if (i < clickers.Count)
            {
                UpdateSingleShopItem(itemUI, clickers[i], i, clickers);
            }
            else
            {
                itemUI.SetVisible(false);
            }
        }
    }
    
    /// <summary>
    /// Updates a single shop item's display and visibility
    /// </summary>
    /// <param name="itemUI">The item UI component</param>
    /// <param name="clicker">Current clicker data</param>
    /// <param name="index">Index of the item</param>
    /// <param name="allClickers">All clicker data for visibility checks</param>
    private void UpdateSingleShopItem(PassiveClickerShopItemUI itemUI, PassiveClickerData clicker, int index, 
        System.Collections.Generic.List<PassiveClickerData> allClickers)
    {
        itemUI.Setup(clicker, index, this);
        
        bool shouldShow = ShouldShopItemBeVisible(index, allClickers);
        itemUI.SetVisible(shouldShow);
        itemUI.SetInteractable(shouldShow);
    }
    
    #endregion

    #region Server Synchronization
    
    /// <summary>
    /// Syncs upgrade purchase with server for tracking purposes
    /// </summary>
    /// <param name="upgradeName">Name of the upgrade</param>
    /// <param name="level">Level of the upgrade</param>
    private async Task SyncUpgradePurchaseAsync(string upgradeName, int level)
    {
        if (!ApiClient.IsTokenValid())
        {
            Debug.LogWarning("[ShopUIManager] Cannot sync upgrade purchase - not logged in");
            return;
        }
        
        try
        {
            string leveledUpgradeName = $"{upgradeName}_Level_{level}";
            Debug.Log($"[ShopUIManager] Syncing upgrade purchase with server: {leveledUpgradeName}");
            
            var response = await ApiClient.PostAsync<object, UpgradeResponse>($"/user/upgrade/{leveledUpgradeName}", null);
            
            if (response != null)
            {
                Debug.Log($"[ShopUIManager] Upgrade purchase synced successfully: {leveledUpgradeName}");
            }
        }
        catch (ApiException ex)
        {
            HandleUpgradeSyncApiException(ex, upgradeName, level);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ShopUIManager] Error syncing upgrade purchase: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handles API exceptions during upgrade sync
    /// </summary>
    /// <param name="ex">The API exception</param>
    /// <param name="upgradeName">Name of the upgrade</param>
    /// <param name="level">Level of the upgrade</param>
    private void HandleUpgradeSyncApiException(ApiException ex, string upgradeName, int level)
    {
        if (ex.StatusCode == 400 && ex.Message.Contains("already owned"))
        {
            Debug.Log($"[ShopUIManager] Upgrade already owned on server: {upgradeName}_Level_{level}");
        }
        else
        {
            Debug.LogWarning($"[ShopUIManager] Failed to sync upgrade purchase with server: {ex.Message}");
        }
    }
    
    #endregion

    #region Private Helpers
    
    /// <summary>
    /// Validates if shop items can be generated
    /// </summary>
    /// <returns>True if generation is possible</returns>
    private bool CanGenerateShopItems()
    {
        if (clickerManager == null)
        {
            Debug.LogWarning("[ShopUIManager] Cannot generate shop items - clickerManager is null");
            return false;
        }

        if (itemListParent == null || itemPrefab == null)
        {
            Debug.LogWarning("[ShopUIManager] Cannot generate shop items - required UI components not assigned");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Validates if shop items can be refreshed
    /// </summary>
    /// <returns>True if refresh is possible</returns>
    private bool CanRefreshShopItems()
    {
        return clickerManager != null && itemListParent != null;
    }
    
    /// <summary>
    /// Validates if a clicker can be purchased
    /// </summary>
    /// <param name="index">Index of the clicker</param>
    /// <returns>True if purchase is possible</returns>
    private bool CanPurchaseClicker(int index)
    {
        if (clickerManager == null)
        {
            Debug.LogWarning("[ShopUIManager] Cannot purchase clicker - clickerManager is null");
            return false;
        }

        var clickers = clickerManager.GetPassiveClickers();
        if (index < 0 || index >= clickers.Count)
        {
            Debug.LogWarning($"[ShopUIManager] Invalid clicker index: {index}");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Gets the current currency amount
    /// </summary>
    /// <returns>Current currency amount</returns>
    private int GetCurrentCurrency()
    {
        return CurrencySyncManager.Instance?.Carrots ?? 0;
    }
    
    /// <summary>
    /// Attempts to upgrade a clicker
    /// </summary>
    /// <param name="index">Index of the clicker to upgrade</param>
    /// <param name="currentCarrots">Current currency amount</param>
    /// <returns>True if upgrade was successful</returns>
    private async Task<bool> AttemptClickerUpgrade(int index, int currentCarrots)
    {
        return await clickerManager.UpgradeClicker(index, currentCarrots);
    }
    
    /// <summary>
    /// Handles successful purchase operations
    /// </summary>
    /// <param name="clicker">The purchased clicker</param>
    /// <param name="index">Index of the clicker</param>
    private async Task HandleSuccessfulPurchase(PassiveClickerData clicker, int index)
    {
        UpdatePurchasedItemUI(clicker, index);
        RefreshShopItems();
        await SyncUpgradePurchaseAsync(clicker.name, clicker.level);
    }
    
    /// <summary>
    /// Updates the UI for a purchased item
    /// </summary>
    /// <param name="clicker">The purchased clicker</param>
    /// <param name="index">Index of the clicker</param>
    private void UpdatePurchasedItemUI(PassiveClickerData clicker, int index)
    {
        var itemUI = GetShopItemUI(index);
        if (itemUI != null)
        {
            itemUI.Setup(clicker, index, this);
        }
    }
    
    /// <summary>
    /// Gets the shop item UI component at the specified index
    /// </summary>
    /// <param name="index">Index of the item</param>
    /// <returns>The UI component or null if not found</returns>
    private PassiveClickerShopItemUI GetShopItemUI(int index)
    {
        if (index < 0 || index >= itemListParent.childCount)
            return null;
            
        return itemListParent.GetChild(index).GetComponent<PassiveClickerShopItemUI>();
    }
    
    /// <summary>
    /// Determines if a shop item should be visible based on unlock rules
    /// Rule: First item always visible, subsequent items visible only if previous item has level >= 1
    /// </summary>
    /// <param name="index">Index of the item to check</param>
    /// <param name="clickers">All clicker data</param>
    /// <returns>True if the item should be visible</returns>
    private bool ShouldShopItemBeVisible(int index, System.Collections.Generic.List<PassiveClickerData> clickers)
    {
        if (index == 0)
            return true;
            
        if (index - 1 < clickers.Count)
        {
            return clickers[index - 1].level >= 1;
        }
        
        return false;
    }
    
    #endregion
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