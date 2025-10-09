using UnityEngine;
using UnityEngine.UI;

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

    public void OnBuyClicker(int index)
    {
        var clicker = clickerManager.GetPassiveClickers()[index];
        int currentCarrots = CurrencySyncManager.Instance != null ? CurrencySyncManager.Instance.Carrots : 0;
        if (clickerManager.UpgradeClicker(index, currentCarrots))
        {
            var itemUI = itemListParent.GetChild(index).GetComponent<PassiveClickerShopItemUI>();
            itemUI.Setup(clicker, index, this);
            RefreshShopItems();
        }
    }
}