using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for individual clan cards in the clan list
/// Attach this to your clan card prefab and assign the UI components
/// The entire card acts as a button to open the clan details modal
/// </summary>
public class ClanCard : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text memberCountText;
    
    [Header("Card Button")]
    [SerializeField] private Button cardButton; // The main button component for the entire card
    
    [Header("Visual Elements")]
    [SerializeField] private Image clanIcon; // Optional clan icon
    
    private ClanData clanData;
    private ClanManager clanManager;
    
    public void SetupClan(ClanData clan, ClanManager manager)
    {
        clanData = clan;
        clanManager = manager;
        UpdateDisplay();
        SetupButtons();
    }
    
    private void UpdateDisplay()
    {
        if (clanData == null) return;
        
        // Set text fields
        if (clanNameText != null) clanNameText.text = clanData.name;
        if (memberCountText != null) memberCountText.text = $"{clanData.memberCount}/50";
    }
    
    private void SetupButtons()
    {
        // Setup the main card button - entire card is clickable
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }
        else
        {
            Debug.LogWarning("Card button is not assigned! The clan card won't be clickable.");
        }
    }
    
    private void OnCardClicked()
    {
        Debug.Log($"Clan card clicked for: {clanData.name} ({clanData.tag})");
        
        if (clanManager != null && clanData != null)
        {
            clanManager.ShowClanDetails(clanData);
        }
        else
        {
            if (clanManager == null) Debug.LogError("ClanManager reference is null!");
            if (clanData == null) Debug.LogError("ClanData is null!");
        }
    }
    
    /// <summary>
    /// Get the clan data associated with this card
    /// </summary>
    public ClanData GetClanData()
    {
        return clanData;
    }
    
    /// <summary>
    /// Refresh the display with updated clan data
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateDisplay();
    }
}