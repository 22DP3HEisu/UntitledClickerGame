using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveUpgradeShopItemUI : MonoBehaviour
{
    [SerializeField] private Image upgradeImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image wall;

    private int upgradeIndex;

    public void Setup(PassiveUpgradeData data, int index)
    {
        upgradeIndex = index;

        if (upgradeImage != null)
            upgradeImage.sprite = data?.icon;

        if (nameText != null)
            nameText.text = data?.upgradeName ?? "";

        if (descText != null)
            descText.text = data?.description ?? "";

        if (priceText != null)
            priceText.text = data != null ? $"Price: {data.price}" : "";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            if (PassiveUpgradeManager.Instance == null)
            {
                Debug.LogWarning("PassiveUpgradeManager.Instance is null. Cannot purchase upgrade.");
                return;
            }

            bool purchased = PassiveUpgradeManager.Instance.PurchaseUpgrade(upgradeIndex);
            if (purchased)
            {
                SetInteractable(false);
                if (priceText != null) priceText.text = "Purchased";
            }
        });

        // set initial interactable based on purchase state
        if (data != null)
            SetInteractable(!PassiveUpgradeManager.Instance?.IsPurchased(data.id) ?? true);
    }

    public void SetInteractable(bool interactable)
    {
        if (buyButton != null)
            buyButton.interactable = interactable;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}