using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example integration showing how to use CurrencySyncManager in your game.
/// This script demonstrates currency display, spending, and sync event handling.
/// </summary>
public class CurrencyIntegrationExample : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text carrotsText;
    [SerializeField] private TMP_Text horseShoesText;
    [SerializeField] private TMP_Text goldenCarrotsText;
    [SerializeField] private TMP_Text syncStatusText;
    
    [Header("Test Buttons")]
    [SerializeField] private Button addCarrotsButton;
    [SerializeField] private Button spendCarrotsButton;
    [SerializeField] private Button manualSyncButton;
    
    private CurrencySyncManager currencyManager;
    
    private void Start()
    {
        currencyManager = CurrencySyncManager.Instance;
        
        if (currencyManager == null)
        {
            Debug.LogError("CurrencySyncManager not found! Make sure it's in the scene.");
            return;
        }
        
        SetupButtons();
        SetupEventListeners();
        UpdateUI();
    }
    
    private void SetupButtons()
    {
        addCarrotsButton?.onClick.AddListener(() => {
            currencyManager.AddCurrency(100, 5, 1);
            UpdateUI();
        });
        
        spendCarrotsButton?.onClick.AddListener(() => {
            bool success = currencyManager.SpendCurrency(50, 2, 0);
            if (success)
            {
                Debug.Log("Purchase successful!");
            }
            else
            {
                Debug.Log("Insufficient currency!");
            }
            UpdateUI();
        });
        
        manualSyncButton?.onClick.AddListener(async () => {
            bool success = await currencyManager.SyncCurrencyAsync();
            UpdateSyncStatus(success ? "Sync successful!" : "Sync failed!");
        });
    }
    
    private void SetupEventListeners()
    {
        // Listen for sync events
        CurrencySyncManager.OnCurrencySynced += OnCurrencySynced;
        CurrencySyncManager.OnSyncFailed += OnSyncFailed;
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        CurrencySyncManager.OnCurrencySynced -= OnCurrencySynced;
        CurrencySyncManager.OnSyncFailed -= OnSyncFailed;
        
        // Clean up button listeners
        addCarrotsButton?.onClick.RemoveAllListeners();
        spendCarrotsButton?.onClick.RemoveAllListeners();
        manualSyncButton?.onClick.RemoveAllListeners();
    }
    
    private void UpdateUI()
    {
        if (currencyManager == null) return;
        
        if (carrotsText != null)
            carrotsText.text = $"Carrots: {currencyManager.Carrots:N0}";
            
        if (horseShoesText != null)
            horseShoesText.text = $"Horse Shoes: {currencyManager.HorseShoes:N0}";
            
        if (goldenCarrotsText != null)
            goldenCarrotsText.text = $"Golden Carrots: {currencyManager.GoldenCarrots:N0}";
    }
    
    private void OnCurrencySynced(CurrencyData currency)
    {
        UpdateUI();
        UpdateSyncStatus($"Synced at {currency.lastSyncAt}");
        Debug.Log("Currency synchronized with server!");
    }
    
    private void OnSyncFailed(string error)
    {
        UpdateSyncStatus($"Sync failed: {error}");
        Debug.LogWarning($"Currency sync failed: {error}");
    }
    
    private void UpdateSyncStatus(string status)
    {
        if (syncStatusText != null)
        {
            syncStatusText.text = status;
        }
    }
    
    // Example of how other systems might interact with currency
    public void OnPlayerEarnedReward(int carrots, int horseShoes = 0, int goldenCarrots = 0)
    {
        currencyManager.AddCurrency(carrots, horseShoes, goldenCarrots);
        UpdateUI();
        
        // Show reward popup or animation here
        Debug.Log($"Player earned: {carrots} carrots, {horseShoes} horse shoes, {goldenCarrots} golden carrots");
    }
    
    public bool TryPurchase(int carrotCost, int horseShoeCost = 0, int goldenCarrotCost = 0)
    {
        bool success = currencyManager.SpendCurrency(carrotCost, horseShoeCost, goldenCarrotCost);
        
        if (success)
        {
            UpdateUI();
            // Handle successful purchase
            Debug.Log("Purchase successful!");
        }
        else
        {
            // Show "insufficient funds" message
            Debug.Log("Insufficient currency for purchase!");
        }
        
        return success;
    }
    
    // Update UI every few seconds to show real-time changes
    private void Update()
    {
        // Update UI periodically in case currency changes elsewhere
        if (Time.frameCount % 180 == 0) // Every 3 seconds at 60 FPS
        {
            UpdateUI();
        }
    }
}

/// <summary>
/// Static helper class for easy currency operations throughout the game
/// </summary>
public static class CurrencyHelper
{
    /// <summary>
    /// Quick access to currency manager
    /// </summary>
    public static CurrencySyncManager Currency => CurrencySyncManager.Instance;
    
    /// <summary>
    /// Check if player can afford a purchase
    /// </summary>
    public static bool CanAfford(int carrots, int horseShoes = 0, int goldenCarrots = 0)
    {
        if (Currency == null) return false;
        
        return Currency.Carrots >= carrots && 
               Currency.HorseShoes >= horseShoes && 
               Currency.GoldenCarrots >= goldenCarrots;
    }
    
    /// <summary>
    /// Get current currency as formatted strings
    /// </summary>
    public static string GetFormattedCurrency()
    {
        if (Currency == null) return "Currency Manager not found";
        
        return $"Carrots: {Currency.Carrots:N0} | Horse Shoes: {Currency.HorseShoes:N0} | Golden Carrots: {Currency.GoldenCarrots:N0}";
    }
    
    /// <summary>
    /// Award currency with logging
    /// </summary>
    public static void AwardCurrency(int carrots, int horseShoes = 0, int goldenCarrots = 0, string reason = "")
    {
        Currency?.AddCurrency(carrots, horseShoes, goldenCarrots);
        
        string logMessage = $"Awarded: {carrots} carrots";
        if (horseShoes > 0) logMessage += $", {horseShoes} horse shoes";
        if (goldenCarrots > 0) logMessage += $", {goldenCarrots} golden carrots";
        if (!string.IsNullOrEmpty(reason)) logMessage += $" ({reason})";
        
        Debug.Log(logMessage);
    }
}