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

    private int clickerIndex;
    private ShopUIManager shopManager;


    public void Setup(PassiveClickerData data, int index, ShopUIManager manager)
    {

        clickerImage.sprite = data.image;
        nameText.text = data.name;
        descText.text = data.description;
        priceText.text = $"Price: {data.GetCurrentPrice()}";
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

}