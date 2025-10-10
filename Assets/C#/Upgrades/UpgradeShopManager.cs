using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeShopManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Parent transform under which upgrade item instances will be created")]
    [SerializeField] private Transform upgradeListParent;

    [Tooltip("Prefab for a single upgrade item (must contain PassiveUpgradeShopItemUI)")]
    [SerializeField] private GameObject upgradeItemPrefab;

    [Header("Options")]
    [Tooltip("If true the manager will attempt to auto-find the PassiveUpgradeManager in scene")]
    [SerializeField] private bool ensureUpgradeManager = true;

    private PassiveUpgradeManager upgradeManager;

    private void Awake()
    {
        // ensure manager reference
        upgradeManager = FindObjectOfType<PassiveUpgradeManager>(true);
        if (upgradeManager == null)
        {
            if (ensureUpgradeManager)
                Debug.LogWarning("[UpgradeShopManager] PassiveUpgradeManager not found in scene.");
            else
                Debug.Log("[UpgradeShopManager] PassiveUpgradeManager not found (ensureUpgradeManager=false).");
        }

        if (upgradeListParent == null)
            Debug.LogWarning("[UpgradeShopManager] upgradeListParent is not assigned in Inspector. Generation will be skipped.");

        if (upgradeItemPrefab == null)
            Debug.LogWarning("[UpgradeShopManager] upgradeItemPrefab is not assigned in Inspector. Generation will be skipped.");
    }

    private void Start()
    {
        if (upgradeManager != null)
            upgradeManager.OnUpgradePurchased += HandleUpgradePurchased;

        GenerateUpgradeItems();
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
    }

    private void HandleUpgradePurchased(PassiveUpgradeData up)
    {
        // refresh UI when purchase happens
        RefreshUpgradeItems();
    }

    public void GenerateUpgradeItems()
    {
        if (upgradeManager == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot generate upgrades - upgradeManager is null.");
            return;
        }

        if (upgradeListParent == null || upgradeItemPrefab == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot generate upgrades - upgradeListParent or upgradeItemPrefab not assigned.");
            return;
        }

        var upgrades = upgradeManager.GetAllUpgrades();
        if (upgrades == null || upgrades.Count == 0)
        {
            Debug.Log("[UpgradeShopManager] No upgrades defined.");
            return;
        }

        // Clear existing children
        for (int i = upgradeListParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(upgradeListParent.GetChild(i).gameObject);

        for (int i = 0; i < upgrades.Count; i++)
        {
            var go = Instantiate(upgradeItemPrefab, upgradeListParent);
            go.SetActive(true);
            var ui = go.GetComponent<PassiveUpgradeShopItemUI>();
            if (ui != null)
            {
                ui.Setup(upgrades[i], i);

                // Visibility rule: first upgrade always visible, subsequent upgrades visible only if previous upgrade purchased
                bool visible = (i == 0) || upgradeManager.IsPurchased(upgrades[i - 1].id);
                ui.SetVisible(visible);
            }
            else
                Debug.LogWarning($"[UpgradeShopManager] Instantiated prefab missing PassiveUpgradeShopItemUI component at index {i}.");
        }

        RefreshUpgradeItems();
    }

    public void RefreshUpgradeItems()
    {
        if (upgradeManager == null || upgradeListParent == null) return;

        var upgrades = upgradeManager.GetAllUpgrades();
        for (int i = 0; i < upgradeListParent.childCount; i++)
        {
            var child = upgradeListParent.GetChild(i);
            var ui = child.GetComponent<PassiveUpgradeShopItemUI>();
            if (ui == null) continue;

            if (i < upgrades.Count)
            {
                // Apply same visibility rule as generation
                bool visible = (i == 0) || upgradeManager.IsPurchased(upgrades[i - 1].id);
                if (visible)
                {
                    ui.Setup(upgrades[i], i);
                    ui.SetVisible(true);
                }
                else
                {
                    ui.SetVisible(false);
                }
            }
            else
            {
                ui.SetVisible(false);
            }
        }
    }

    public bool TryPurchaseUpgrade(int index)
    {
        if (upgradeManager == null)
        {
            Debug.LogWarning("[UpgradeShopManager] Cannot purchase - upgradeManager is null.");
            return false;
        }

        bool ok = upgradeManager.PurchaseUpgrade(index);
        if (ok)
        {
            RefreshUpgradeItems();
            return true;
        }
        return false;
    }
}