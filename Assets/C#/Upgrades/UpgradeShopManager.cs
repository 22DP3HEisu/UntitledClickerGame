using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Manages the upgrade shop UI, handles upgrade item generation, visibility, and purchase interactions
public class UpgradeShopManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("UI References")]
    [Tooltip("Parent transform under which upgrade item instances will be created")]
    [SerializeField] private Transform upgradeListParent;

    [Tooltip("Prefab for a single upgrade item (must contain PassiveUpgradeShopItemUI)")]
    [SerializeField] private GameObject upgradeItemPrefab;

    [Header("Configuration")]
    [Tooltip("If true the manager will attempt to auto-find the PassiveUpgradeManager in scene")]
    [SerializeField] private bool ensureUpgradeManager = true;
    
    #endregion

    #region Private Fields
    
    private PassiveUpgradeManager upgradeManager;
    
    #endregion

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeUpgradeManager();
        ValidateRequiredComponents();
    }

    private void Start()
    {
        SubscribeToEvents();
        GenerateUpgradeItems();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion

    #region Initialization
    
    // Initializes the upgrade manager reference
    private void InitializeUpgradeManager()
    {
        upgradeManager = FindObjectOfType<PassiveUpgradeManager>(true);
        
        if (upgradeManager == null)
        {
            if (ensureUpgradeManager)
            {
                Debug.LogWarning("[UpgradeShopManager] PassiveUpgradeManager not found in scene.");
            }
            else
            {
                Debug.Log("[UpgradeShopManager] PassiveUpgradeManager not found (ensureUpgradeManager=false).");
            }
        }
    }
    
    // Validates that required components are assigned in the inspector
    private void ValidateRequiredComponents()
    {
        if (upgradeListParent == null)
        {
            Debug.LogWarning("[UpgradeShopManager] upgradeListParent is not assigned in Inspector. Generation will be skipped.");
        }

        if (upgradeItemPrefab == null)
        {
            Debug.LogWarning("[UpgradeShopManager] upgradeItemPrefab is not assigned in Inspector. Generation will be skipped.");
        }
    }
    
    // Subscribes to necessary events
    private void SubscribeToEvents()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        CurrencySyncManager.OnGameDataLoaded += HandleGameDataLoaded;
    }
    
    // Unsubscribes from events to prevent memory leaks
    private void UnsubscribeFromEvents()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }
        
        CurrencySyncManager.OnGameDataLoaded -= HandleGameDataLoaded;
    }
    
    #endregion

    #region Event Handlers
    
    // Handles upgrade purchase events by refreshing the UI
    // <param name="purchasedUpgrade">The upgrade that was purchased</param>
    private void HandleUpgradePurchased(PassiveUpgradeData purchasedUpgrade)
    {
        RefreshUpgradeItems();
    }
    
    // Handles game data loaded events by refreshing the upgrade UI
    private void HandleGameDataLoaded()
    {
        Debug.Log("[UpgradeShopManager] Game data loaded - refreshing upgrade UI");
        RefreshUpgradeItems();
    }
    
    #endregion

    #region Public Interface
    
    // Generates all upgrade items in the shop UI based on available upgrades
    public void GenerateUpgradeItems()
    {
        if (!CanGenerateUpgradeItems())
            return;

        var upgrades = upgradeManager.GetAllUpgrades();
        if (!HasValidUpgrades(upgrades))
            return;

        ClearExistingUpgradeItems();
        CreateUpgradeItems(upgrades);
        RefreshUpgradeItems();
    }

    // Refreshes the visibility and state of all upgrade items
    public void RefreshUpgradeItems()
    {
        if (!CanRefreshUpgradeItems())
            return;

        var upgrades = upgradeManager.GetAllUpgrades();
        UpdateUpgradeItemsVisibility(upgrades);
    }


    // Attempts to purchase an upgrade by index
    // <param name="index">Index of the upgrade to purchase</param>
    // <returns>True if purchase was successful, false otherwise</returns>
    public bool TryPurchaseUpgrade(int index)
    {
        if (upgradeManager == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot purchase - upgradeManager is null.");
            return false;
        }

        bool purchaseSuccessful = upgradeManager.PurchaseUpgrade(index);
        if (purchaseSuccessful)
        {
            RefreshUpgradeItems();
        }
        
        return purchaseSuccessful;
    }
    
    #endregion

    #region Private Helpers
    
    // Validates if upgrade items can be generated
    // <returns>True if generation is possible</returns>
    private bool CanGenerateUpgradeItems()
    {
        if (upgradeManager == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot generate upgrades - upgradeManager is null.");
            return false;
        }

        if (upgradeListParent == null || upgradeItemPrefab == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot generate upgrades - upgradeListParent or upgradeItemPrefab not assigned.");
            return false;
        }

        return true;
    }

    // Validates if refresh operations can be performed
    // <returns>True if refresh is possible</returns>
    private bool CanRefreshUpgradeItems()
    {
        return upgradeManager != null && upgradeListParent != null;
    }
    
    // Checks if the upgrades list is valid and contains items
    // <param name="upgrades">List of upgrades to validate</param>
    // <returns>True if upgrades are valid</returns>
    private bool HasValidUpgrades(List<PassiveUpgradeData> upgrades)
    {
        if (upgrades == null || upgrades.Count == 0)
        {
            Debug.Log("[UpgradeShopManager] No upgrades defined.");
            return false;
        }
        return true;
    }
    
    // Clears all existing upgrade item GameObjects from the parent
    private void ClearExistingUpgradeItems()
    {
        for (int i = upgradeListParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(upgradeListParent.GetChild(i).gameObject);
        }
    }
    
    // Creates upgrade item GameObjects for each upgrade
    // <param name="upgrades">List of upgrades to create items for</param>
    private void CreateUpgradeItems(List<PassiveUpgradeData> upgrades)
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            CreateSingleUpgradeItem(upgrades[i], i);
        }
    }
    
    // Creates a single upgrade item GameObject
    // <param name="upgradeData">Data for the upgrade</param>
    // <param name="index">Index of the upgrade</param>
    private void CreateSingleUpgradeItem(PassiveUpgradeData upgradeData, int index)
    {
        var upgradeObject = Instantiate(upgradeItemPrefab, upgradeListParent);
        upgradeObject.SetActive(true);
        
        var upgradeUI = upgradeObject.GetComponent<PassiveUpgradeShopItemUI>();
        if (upgradeUI != null)
        {
            upgradeUI.Setup(upgradeData, index);
            
            bool isVisible = ShouldUpgradeBeVisible(index);
            upgradeUI.SetVisible(isVisible);
        }
        else
        {
            Debug.LogWarning($"[UpgradeShopManager] Instantiated prefab missing PassiveUpgradeShopItemUI component at index {index}.");
        }
    }
    
    // Updates the visibility of all upgrade items based on purchase status
    // <param name="upgrades">List of upgrade data</param>
    private void UpdateUpgradeItemsVisibility(List<PassiveUpgradeData> upgrades)
    {
        for (int i = 0; i < upgradeListParent.childCount; i++)
        {
            var child = upgradeListParent.GetChild(i);
            var upgradeUI = child.GetComponent<PassiveUpgradeShopItemUI>();
            
            if (upgradeUI == null) continue;

            if (i < upgrades.Count)
            {
                bool isVisible = ShouldUpgradeBeVisible(i);
                if (isVisible)
                {
                    upgradeUI.Setup(upgrades[i], i);
                    upgradeUI.SetVisible(true);
                }
                else
                {
                    upgradeUI.SetVisible(false);
                }
            }
            else
            {
                upgradeUI.SetVisible(false);
            }
        }
    }
    
    // Determines if an upgrade should be visible based on visibility rules
    // Visibility rule: first upgrade always visible, subsequent upgrades visible only if previous upgrade purchased
    // <param name="upgradeIndex">Index of the upgrade to check</param>
    // <returns>True if the upgrade should be visible</returns>
    private bool ShouldUpgradeBeVisible(int upgradeIndex)
    {
        if (upgradeIndex == 0)
            return true;
            
        var upgrades = upgradeManager.GetAllUpgrades();
        if (upgradeIndex - 1 < upgrades.Count)
        {
            return upgradeManager.IsPurchased(upgrades[upgradeIndex - 1].id);
        }
        
        return false;
    }
    
    #endregion
}