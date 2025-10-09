using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// Manages automatic synchronization of currency data with the server.
/// Syncs every 60 seconds to keep client and server data consistent.
/// </summary>
public class CurrencySyncManager : MonoBehaviour
{
    [Header("Sync Settings")]
    [SerializeField] private float syncInterval = 60f; // Sync every 60 seconds
    [SerializeField] private bool enableAutoSync = true;
    [SerializeField] private bool syncOnApplicationPause = true;
    
    [Header("Currency Data")]
    [SerializeField] private int carrots = 0;
    [SerializeField] private int horseShoes = 0;
    [SerializeField] private int goldenCarrots = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Events for other systems to listen to
    public static event Action<CurrencyData> OnCurrencySynced;
    public static event Action<string> OnSyncFailed;
    
    // Properties for accessing currency
    public int Carrots 
    { 
        get => carrots; 
        set { carrots = Mathf.Max(0, value); MarkDirty(); } 
    }
    
    public int HorseShoes 
    { 
        get => horseShoes; 
        set { horseShoes = Mathf.Max(0, value); MarkDirty(); } 
    }
    
    public int GoldenCarrots 
    { 
        get => goldenCarrots; 
        set { goldenCarrots = Mathf.Max(0, value); MarkDirty(); } 
    }
    
    private float lastSyncTime;
    private bool isDirty = false; // Tracks if currency has changed since last sync
    private bool isSyncing = false;
    
    // Singleton pattern for easy access
    public static CurrencySyncManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadLocalCurrency();
    }
    
    private async void Start()
    {
        // If user is already logged in, load currency from server on startup
        if (ApiClient.IsTokenValid())
        {
            LogDebug("User already logged in - loading currency from server");
            await LoadCurrencyFromServer();
        }
        
        // Start sync timer if auto sync is enabled and user is logged in
        if (enableAutoSync && ApiClient.IsTokenValid())
        {
            InvokeRepeating(nameof(TriggerSync), syncInterval, syncInterval);
            LogDebug("Auto currency sync started");
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Sync when app is paused (going to background)
        if (syncOnApplicationPause && pauseStatus && ApiClient.IsTokenValid())
        {
            LogDebug("App paused - syncing currency");
            _ = SyncCurrencyAsync();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // Sync when app loses focus
        if (syncOnApplicationPause && !hasFocus && ApiClient.IsTokenValid())
        {
            LogDebug("App lost focus - syncing currency");
            _ = SyncCurrencyAsync();
        }
    }
    
    private void OnDestroy()
    {
        SaveLocalCurrency();
        
        // Try to sync to server before shutdown (fire and forget)
        if (ApiClient.IsTokenValid() && isDirty)
        {
            _ = SyncCurrencyAsync();
        }
    }
    
    private void TriggerSync()
    {
        if (ApiClient.IsTokenValid() && isDirty && !isSyncing)
        {
            _ = SyncCurrencyAsync();
        }
    }
    
    /// <summary>
    /// Manually trigger a currency sync
    /// </summary>
    public async Task<bool> SyncCurrencyAsync()
    {
        if (!ApiClient.IsTokenValid())
        {
            LogDebug("Cannot sync currency - not logged in");
            return false;
        }
        
        if (isSyncing)
        {
            LogDebug("Sync already in progress");
            return false;
        }
        
        isSyncing = true;
        
        try
        {
            LogDebug($"Syncing currency: Carrots={carrots}, HorseShoes={horseShoes}, GoldenCarrots={goldenCarrots}");
            
            var syncData = new CurrencySyncRequest
            {
                carrots = carrots,
                horseShoes = horseShoes,
                goldenCarrots = goldenCarrots
            };
            
            var response = await ApiClient.PutAsync<CurrencySyncRequest, CurrencySyncResponse>("/user/sync-currency", syncData);
            
            if (response?.currency != null)
            {
                // Update local currency with server response (server is authoritative)
                carrots = response.currency.carrots;
                horseShoes = response.currency.horseShoes;
                goldenCarrots = response.currency.goldenCarrots;
                
                isDirty = false;
                lastSyncTime = Time.time;
                
                SaveLocalCurrency();
                
                LogDebug($"Currency sync successful at {response.currency.lastSyncAt}");
                OnCurrencySynced?.Invoke(response.currency);
                
                return true;
            }
            
            return false;
        }
        catch (ApiException ex)
        {
            string error = $"Currency sync failed: {ex.Message}";
            LogDebug(error);
            OnSyncFailed?.Invoke(error);
            return false;
        }
        catch (Exception ex)
        {
            string error = $"Currency sync error: {ex.Message}";
            LogDebug(error);
            OnSyncFailed?.Invoke(error);
            return false;
        }
        finally
        {
            isSyncing = false;
        }
    }
    
    /// <summary>
    /// Add currency (with automatic sync marking)
    /// </summary>
    public void AddCurrency(int carrotAmount, int horseShoesAmount = 0, int goldenCarrotAmount = 0)
    {
        if (carrotAmount > 0) Carrots += carrotAmount;
        if (horseShoesAmount > 0) HorseShoes += horseShoesAmount;
        if (goldenCarrotAmount > 0) GoldenCarrots += goldenCarrotAmount;
        
        LogDebug($"Added currency: +{carrotAmount} carrots, +{horseShoesAmount} horseshoes, +{goldenCarrotAmount} golden carrots");
    }
    
    /// <summary>
    /// Spend currency (returns true if successful)
    /// </summary>
    public bool SpendCurrency(int carrotCost, int horseShoeCost = 0, int goldenCarrotCost = 0)
    {
        if (carrots >= carrotCost && horseShoes >= horseShoeCost && goldenCarrots >= goldenCarrotCost)
        {
            Carrots -= carrotCost;
            HorseShoes -= horseShoeCost;
            GoldenCarrots -= goldenCarrotCost;
            
            LogDebug($"Spent currency: -{carrotCost} carrots, -{horseShoeCost} horseshoes, -{goldenCarrotCost} golden carrots");
            return true;
        }
        
        LogDebug("Insufficient currency for purchase");
        return false;
    }
    
    /// <summary>
    /// Load currency from server (called on login)
    /// </summary>
    public async Task LoadCurrencyFromServer()
    {
        if (!ApiClient.IsTokenValid()) return;
        
        try
        {
            var profile = await ApiClient.GetAsync<UserProfileResponse>("/user");
            
            if (profile?.user?.gameData != null)
            {
                carrots = profile.user.gameData.carrots;
                horseShoes = profile.user.gameData.horseShoes;
                goldenCarrots = profile.user.gameData.goldenCarrots;
                
                isDirty = false;
                SaveLocalCurrency();
                
                LogDebug("Currency loaded from server");
                
                // Enable auto sync now that we're logged in
                if (enableAutoSync && !IsInvoking(nameof(TriggerSync)))
                {
                    InvokeRepeating(nameof(TriggerSync), syncInterval, syncInterval);
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to load currency from server: {ex.Message}");
        }
    }
    
    private void MarkDirty()
    {
        isDirty = true;
        SaveLocalCurrency(); // Save locally immediately
    }
    
    private void SaveLocalCurrency()
    {
        PlayerPrefs.SetInt("LocalCarrots", carrots);
        PlayerPrefs.SetInt("LocalHorseShoes", horseShoes);
        PlayerPrefs.SetInt("LocalGoldenCarrots", goldenCarrots);
        PlayerPrefs.Save();
    }
    
    private void LoadLocalCurrency()
    {
        carrots = PlayerPrefs.GetInt("LocalCarrots", 0);
        horseShoes = PlayerPrefs.GetInt("LocalHorseShoes", 0);
        goldenCarrots = PlayerPrefs.GetInt("LocalGoldenCarrots", 0);
    }
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CurrencySync] {message}");
        }
    }
    
    // Manual sync button for testing
    [ContextMenu("Manual Sync")]
    public void ManualSync()
    {
        _ = SyncCurrencyAsync();
    }
    
    // Debug methods
    [ContextMenu("Add Test Currency")]
    public void AddTestCurrency()
    {
        AddCurrency(100, 10, 5);
    }
    
    public void EnableAutoSync()
    {
        enableAutoSync = true;
        if (ApiClient.IsTokenValid() && !IsInvoking(nameof(TriggerSync)))
        {
            InvokeRepeating(nameof(TriggerSync), syncInterval, syncInterval);
        }
    }
    
    /// <summary>
    /// Call this method after successful login to load currency from server
    /// </summary>
    public async Task InitializeAfterLogin()
    {
        LogDebug("Initializing currency after login");
        await LoadCurrencyFromServer();
    }
    
    public void DisableAutoSync()
    {
        enableAutoSync = false;
        CancelInvoke(nameof(TriggerSync));
    }
    
    [ContextMenu("Test Server Load")]
    public async void TestServerLoad()
    {
        LogDebug("Testing currency load from server...");
        await LoadCurrencyFromServer();
    }
}

// Data structures for currency sync
[Serializable]
public class CurrencySyncRequest
{
    public int carrots;
    public int horseShoes;
    public int goldenCarrots;
}

[Serializable]
public class CurrencySyncResponse
{
    public string message;
    public CurrencyData currency;
}

[Serializable]
public class CurrencyData
{
    public int carrots;
    public int horseShoes;
    public int goldenCarrots;
    public string lastSyncAt;
}