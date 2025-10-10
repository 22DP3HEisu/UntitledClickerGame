using UnityEngine;
using UnityEngine.UI;

public class ShopTabsController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buildingsButton;
    [SerializeField] private Button upgradesButton;

    [Header("Panels")]
    [SerializeField] private GameObject buildingsPanel;
    [SerializeField] private GameObject upgradesPanel;

    [Header("Settings")]
    [SerializeField] private bool showBuildingsByDefault = true;

    private void Start()
    {
        // Initialize shop tabs
        buildingsPanel.SetActive(showBuildingsByDefault);
        upgradesPanel.SetActive(!showBuildingsByDefault);

        // Add button listeners
        buildingsButton.onClick.AddListener(ShowBuildings);
        upgradesButton.onClick.AddListener(ShowUpgrades);
    }

    private void ShowBuildings()
    {
        buildingsPanel.SetActive(true);
        upgradesPanel.SetActive(false);
    }

    private void ShowUpgrades()
    {
        buildingsPanel.SetActive(false);
        upgradesPanel.SetActive(true);
    }
}