using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component for individual clan cards in the clan list
/// Attach this to your clan card prefab and assign the UI components
/// </summary>
public class ClanCard : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanTagText;
    [SerializeField] private TMP_Text clanDescriptionText;
    [SerializeField] private TMP_Text leaderNameText;
    [SerializeField] private TMP_Text memberCountText;
    [SerializeField] private TMP_Text creationDateText;
    
    [Header("Actions")]
    [SerializeField] private Button viewDetailsButton;
    [SerializeField] private Button quickJoinButton;
    
    [Header("Visual Elements")]
    [SerializeField] private Image clanIcon; // Optional clan icon
    [SerializeField] private GameObject fullIndicator; // Show if clan is full
    
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
        if (clanTagText != null) clanTagText.text = $"[{clanData.tag}]";
        if (clanDescriptionText != null) 
        {
            // Truncate description if too long
            string description = clanData.description;
            if (!string.IsNullOrEmpty(description) && description.Length > 50)
            {
                description = description.Substring(0, 47) + "...";
            }
            clanDescriptionText.text = !string.IsNullOrEmpty(description) ? description : "No description";
        }
        if (leaderNameText != null) leaderNameText.text = $"Leader: {clanData.leaderName}";
        if (memberCountText != null) memberCountText.text = $"Members: {clanData.memberCount}";
        
        // Format creation date
        if (creationDateText != null)
        {
            try
            {
                if (System.DateTime.TryParse(clanData.creationDate, out System.DateTime date))
                {
                    creationDateText.text = $"Created: {date:MMM dd, yyyy}";
                }
                else
                {
                    creationDateText.text = "Created: Unknown";
                }
            }
            catch
            {
                creationDateText.text = "Created: Unknown";
            }
        }
        
        // Show full indicator if clan has many members (assuming max is around 50)
        if (fullIndicator != null)
        {
            fullIndicator.SetActive(clanData.memberCount >= 50);
        }
    }
    
    private void SetupButtons()
    {
        // Setup button listeners
        if (viewDetailsButton != null)
        {
            viewDetailsButton.onClick.RemoveAllListeners();
            viewDetailsButton.onClick.AddListener(ViewClanDetails);
        }
        
        if (quickJoinButton != null)
        {
            quickJoinButton.onClick.RemoveAllListeners();
            quickJoinButton.onClick.AddListener(QuickJoinClan);
        }
    }
    
    private void ViewClanDetails()
    {
        Debug.Log($"Viewing details for clan: {clanData.name}");
        
        if (clanManager != null)
        {
            clanManager.ShowClanDetails(clanData);
        }
        else
        {
            Debug.LogError("ClanManager reference is null!");
        }
    }
    
    private async void QuickJoinClan()
    {
        Debug.Log($"Quick joining clan: {clanData.name}");
        
        // For now, just show the details modal
        // Later you can implement quick join functionality
        ViewClanDetails();
        
        // Example of how quick join might work:
        /*
        try
        {
            var response = await ApiClient.PostAsync<object, JoinClanResponse>($"/clans/{clanData.id}/join", null);
            if (response != null)
            {
                Debug.Log($"Successfully joined clan: {clanData.name}");
                // Update UI or show success message
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to join clan: {ex.Message}");
        }
        */
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