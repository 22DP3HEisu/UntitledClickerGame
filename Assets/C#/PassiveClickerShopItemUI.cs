using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveClickerShopItemUI : MonoBehaviour
{
    [SerializeField] private Image clickerImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image wall;

    private int clickerIndex;
    private ShopUIManager shopManager;

    public void Setup(PassiveClickerData data, int index, ShopUIManager manager)
    {
        clickerImage.sprite = data.image;
        nameText.text = data.name;
        descText.text = data.description;

        // Use formatted price
        priceText.text = $"Price: {FormatNumber(data.GetCurrentPrice())}";
        levelText.text = $"Level: {data.level}";

        clickerIndex = index;
        shopManager = manager;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => {
            shopManager.OnBuyClicker(clickerIndex);
        });
    }

    public void SetInteractable(bool interactable)
    {
        buyButton.interactable = interactable;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // 🔹 Helper method to format large numbers
    private string FormatNumber(double num)
    {
        if (num < 1000) return num.ToString("0");
        if (num < 1_000_000) return (num / 1_000d).ToString("0.#") + "k";
        if (num < 1_000_000_000) return (num / 1_000_000d).ToString("0.#") + "M";
        if (num < 1_000_000_000_000) return (num / 1_000_000_000d).ToString("0.#") + "B";
        if (num < 1_000_000_000_000_000) return (num / 1_000_000_000_000d).ToString("0.#") + "T";
        return num.ToString("0.#e0"); // fallback for extremely large numbers
    }
}
