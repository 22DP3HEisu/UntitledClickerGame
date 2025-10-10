using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the upgrade shop UI, handles upgrade item generation, visibility, and purchase interactions
/// </summary>
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
    
    /// <summary>
    /// Initializes the upgrade manager reference
    /// </summary>
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
    
    /// <summary>
    /// Validates that required components are assigned in the inspector
    /// </summary>
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
    
    /// <summary>
    /// Subscribes to necessary events
    /// </summary>
    private void SubscribeToEvents()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        CurrencySyncManager.OnGameDataLoaded += HandleGameDataLoaded;
    }
    
    /// <summary>
    /// Unsubscribes from events to prevent memory leaks
    /// </summary>
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
    
    /// <summary>
    /// Handles upgrade purchase events by refreshing the UI
    /// </summary>
    /// <param name="purchasedUpgrade">The upgrade that was purchased</param>
    private void HandleUpgradePurchased(PassiveUpgradeData purchasedUpgrade)
    {
        RefreshUpgradeItems();
    }
    
    /// <summary>
    /// Handles game data loaded events by refreshing the upgrade UI
    /// </summary>
    private void HandleGameDataLoaded()
    {
        Debug.Log("[UpgradeShopManager] Game data loaded - refreshing upgrade UI");
        RefreshUpgradeItems();
    }
    
    #endregion

    #region Public Interface
    
    /// <summary>
    /// Generates all upgrade items in the shop UI based on available upgrades
    /// </summary>
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
    
    /// <summary>
    /// Refreshes the visibility and state of all upgrade items
    /// </summary>
    public void RefreshUpgradeItems()
    {
        if (!CanRefreshUpgradeItems())
            return;

        var upgrades = upgradeManager.GetAllUpgrades();
        UpdateUpgradeItemsVisibility(upgrades);
    }

    /// <summary>
    /// Attempts to purchase an upgrade by index
    /// </summary>
    /// <param name="index">Index of the upgrade to purchase</param>
    /// <returns>True if purchase was successful, false otherwise</returns>
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
    
    /// <summary>
    /// Validates if upgrade items can be generated
    /// </summary>
    /// <returns>True if generation is possible</returns>
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
    
    /// <summary>
    /// Validates if refresh operations can be performed
    /// </summary>
    /// <returns>True if refresh is possible</returns>
    private bool CanRefreshUpgradeItems()
    {
        return upgradeManager != null && upgradeListParent != null;
    }
    
    /// <summary>
    /// Checks if the upgrades list is valid and contains items
    /// </summary>
    /// <param name="upgrades">List of upgrades to validate</param>
    /// <returns>True if upgrades are valid</returns>
    private bool HasValidUpgrades(List<PassiveUpgradeData> upgrades)
    {
        if (upgrades == null || upgrades.Count == 0)
        {
            Debug.Log("[UpgradeShopManager] No upgrades defined.");
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Clears all existing upgrade item GameObjects from the parent
    /// </summary>
    private void ClearExistingUpgradeItems()
    {
        for (int i = upgradeListParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(upgradeListParent.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// Creates upgrade item GameObjects for each upgrade
    /// </summary>
    /// <param name="upgrades">List of upgrades to create items for</param>
    private void CreateUpgradeItems(List<PassiveUpgradeData> upgrades)
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            CreateSingleUpgradeItem(upgrades[i], i);
        }
    }
    
    /// <summary>
    /// Creates a single upgrade item GameObject
    /// </summary>
    /// <param name="upgradeData">Data for the upgrade</param>
    /// <param name="index">Index of the upgrade</param>
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
    
    /// <summary>
    /// Updates the visibility of all upgrade items based on purchase status
    /// </summary>
    /// <param name="upgrades">List of upgrade data</param>
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
    
    /// <summary>
    /// Determines if an upgrade should be visible based on visibility rules
    /// Visibility rule: first upgrade always visible, subsequent upgrades visible only if previous upgrade purchased
    /// </summary>
    /// <param name="upgradeIndex">Index of the upgrade to check</param>
    /// <returns>True if the upgrade should be visible</returns>
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