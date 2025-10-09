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

    // Preserve original color blocks so we can restore visuals without disabling interactability
    private ColorBlock buildingsOriginalColors;
    private ColorBlock upgradesOriginalColors;

    private void Awake()
    {
        // cache original colors
        if (buildingsButton != null) buildingsOriginalColors = buildingsButton.colors;
        if (upgradesButton != null) upgradesOriginalColors = upgradesButton.colors;

        // Wire up listeners (defensive)
        if (buildingsButton != null)
        {
            buildingsButton.onClick.RemoveAllListeners();
            buildingsButton.onClick.AddListener(() =>
            {
                Debug.Log("[ShopTabsController] Buildings button clicked");
                ShowBuildingsPanel();
            });
        }

        if (upgradesButton != null)
        {
            upgradesButton.onClick.RemoveAllListeners();
            upgradesButton.onClick.AddListener(() =>
            {
                Debug.Log("[ShopTabsController] Upgrades button clicked");
                ShowUpgradesPanel();
            });
        }
    }

    private void Start()
    {
        // Ensure buttons remain interactable at start
        ForceEnableButtons();

        // Initialize panels according to the default setting
        if (showBuildingsByDefault)
            ShowBuildingsPanel();
        else
            ShowUpgradesPanel();

        LogButtonStates("Start");
    }

    // Keep buttons interactable; use visual feedback for the active tab
    public void ShowBuildingsPanel()
    {
        if (buildingsPanel != null) buildingsPanel.SetActive(true);
        if (upgradesPanel != null) upgradesPanel.SetActive(false);

        // Bring active panel to front
        if (buildingsPanel != null) buildingsPanel.transform.SetAsLastSibling();

        // Keep interactable true but update visuals
        if (buildingsButton != null) ApplyActiveVisual(buildingsButton, true);
        if (upgradesButton != null) ApplyActiveVisual(upgradesButton, false);

        Debug.Log("[ShopTabsController] ShowBuildingsPanel executed");
        LogButtonStates("ShowBuildingsPanel");
    }

    public void ShowUpgradesPanel()
    {
        if (upgradesPanel != null) upgradesPanel.SetActive(true);
        if (buildingsPanel != null) buildingsPanel.SetActive(false);

        if (upgradesPanel != null) upgradesPanel.transform.SetAsLastSibling();

        if (upgradesButton != null) ApplyActiveVisual(upgradesButton, true);
        if (buildingsButton != null) ApplyActiveVisual(buildingsButton, false);

        Debug.Log("[ShopTabsController] ShowUpgradesPanel executed");
        LogButtonStates("ShowUpgradesPanel");
    }

    // Ensure buttons are interactable (callable from other scripts if needed)
    public void ForceEnableButtons()
    {
        if (buildingsButton != null)
        {
            buildingsButton.interactable = true;
            buildingsButton.colors = buildingsOriginalColors;
        }

        if (upgradesButton != null)
        {
            upgradesButton.interactable = true;
            upgradesButton.colors = upgradesOriginalColors;
        }

        Debug.Log("[ShopTabsController] ForceEnableButtons called");
    }

    // Update visual state without changing interactable
    private void ApplyActiveVisual(Button btn, bool active)
    {
        if (btn == null) return;

        btn.interactable = true; // keep clickable
        ColorBlock cb = btn.colors;

        if (active)
        {
            // Slightly dim or tint active button to indicate selected state (does not disable)
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = cb.normalColor;
        }
        else
        {
            // restore original per-button colors
            if (btn == buildingsButton) cb = buildingsOriginalColors;
            else if (btn == upgradesButton) cb = upgradesOriginalColors;
        }

        btn.colors = cb;
    }

    private void LogButtonStates(string when)
    {
        string bState = buildingsButton != null ? $"active={buildingsButton.gameObject.activeInHierarchy}, interactable={buildingsButton.interactable}" : "null";
        string uState = upgradesButton != null ? $"active={upgradesButton.gameObject.activeInHierarchy}, interactable={upgradesButton.interactable}" : "null";
        Debug.Log($"[ShopTabsController] {when} - BuildingsButton: {bState}, UpgradesButton: {uState}");
    }

    private void OnDestroy()
    {
        if (buildingsButton != null) buildingsButton.onClick.RemoveAllListeners();
        if (upgradesButton != null) upgradesButton.onClick.RemoveAllListeners();
    }
}