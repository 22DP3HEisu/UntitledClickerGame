using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// Handles individual clan member items in the member list
/// Manages member display, role management, and member actions
/// </summary>
public class ClanMemberItem : MonoBehaviour
{
    [Header("Member Display")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text roleText;
    
    [Header("Member Actions")]
    [SerializeField] private Button promoteButton;
    [SerializeField] private Button demoteButton;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button donateButton;
    
    [Header("Donation Settings")]
    [SerializeField] private TMP_InputField donationAmountInput;
    [SerializeField] private int minDonationAmount = 1;
    [SerializeField] private int maxDonationAmount = 10000;
    
    // Member data
    private ClanMember memberData;
    private ClanDetailData clanData;
    private UserProfileResponse.UserProfile currentUser;
    private ClanDetailModal parentModal;
    
    // Events
    public event Action<ClanMemberItem> OnMemberActionCompleted;
    
    #region Public Interface
    
    /// <summary>
    /// Setup the member item with data and references
    /// </summary>
    public void SetupMember(ClanMember member, ClanDetailData clan, UserProfileResponse.UserProfile user, ClanDetailModal modal)
    {
        memberData = member;
        clanData = clan;
        currentUser = user;
        parentModal = modal;
        
        DisplayMemberInfo();
        SetupButtons();
        DetermineButtonVisibility();
    }
    
    /// <summary>
    /// Get the member data associated with this item
    /// </summary>
    public ClanMember GetMemberData()
    {
        return memberData;
    }
    
    #endregion
    
    #region UI Display
    
    private void DisplayMemberInfo()
    {
        if (memberData == null) return;
        
        // Display username
        if (usernameText != null)
        {
            usernameText.text = memberData.username;
        }
        
        // Display role
        if (roleText != null)
        {
            roleText.text = GetMemberRole();
        }
    }
    
    private string GetMemberRole()
    {
        if (memberData.isLeader)
        {
            return "Leader";
        }
        
        // We don't have officer rank info in current structure, assume Member for now
        // This can be extended when officer rank is added to the API
        return "Member";
    }
    
    #endregion
    
    #region Button Management
    
    private void SetupButtons()
    {
        // Setup promote button
        if (promoteButton != null)
        {
            promoteButton.onClick.RemoveAllListeners();
            promoteButton.onClick.AddListener(() => _ = PromoteMemberAsync());
        }
        
        // Setup demote button
        if (demoteButton != null)
        {
            demoteButton.onClick.RemoveAllListeners();
            demoteButton.onClick.AddListener(() => _ = DemoteMemberAsync());
        }
        
        // Setup kick button
        if (kickButton != null)
        {
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(() => _ = KickMemberAsync());
        }
        
        // Setup donate button
        if (donateButton != null)
        {
            donateButton.onClick.RemoveAllListeners();
            donateButton.onClick.AddListener(() => _ = DonateToDonationAmountAsync());
        }
        
        // Setup donation input validation
        if (donationAmountInput != null)
        {
            donationAmountInput.onValueChanged.AddListener(ValidateDonationInput);
        }
    }
    
    private void DetermineButtonVisibility()
    {
        if (currentUser == null || memberData == null || clanData == null)
        {
            HideAllButtons();
            return;
        }
        
        bool isCurrentUser = currentUser.id == memberData.id;
        bool isCurrentUserLeader = IsCurrentUserLeader();
        bool isTargetLeader = memberData.isLeader;
        
        // Hide all buttons for self
        if (isCurrentUser)
        {
            HideAllButtons();
            return;
        }
        
        // Only leaders can manage other members
        if (!isCurrentUserLeader)
        {
            HideAllButtons();
            return;
        }
        
        // Leaders can't be kicked, promoted, or demoted by other leaders
        if (isTargetLeader)
        {
            SetButtonVisibility(promoteButton, false);
            SetButtonVisibility(demoteButton, false);
            SetButtonVisibility(kickButton, false);
            SetButtonVisibility(donateButton, true); // Can still donate to leader
        }
        else
        {
            // Regular members can be managed by leaders
            SetButtonVisibility(promoteButton, true);
            SetButtonVisibility(demoteButton, false); // Can't demote regular members
            SetButtonVisibility(kickButton, true);
            SetButtonVisibility(donateButton, true);
        }
    }
    
    private void HideAllButtons()
    {
        SetButtonVisibility(promoteButton, false);
        SetButtonVisibility(demoteButton, false);
        SetButtonVisibility(kickButton, false);
        SetButtonVisibility(donateButton, false);
    }
    
    private void SetButtonVisibility(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }
    
    private bool IsCurrentUserLeader()
    {
        if (currentUser == null || clanData == null) return false;
        
        // Check if current user is the clan leader
        var currentUserMember = Array.Find(clanData.members, m => m.id == currentUser.id);
        return currentUserMember?.isLeader ?? false;
    }
    
    #endregion
    
    #region Member Actions
    
    private async Task PromoteMemberAsync()
    {
        if (memberData == null || clanData == null) return;
        
        try
        {
            ShowMemberStatus("Promoting member...", false);
            SetButtonInteractable(promoteButton, false);
            
            var requestData = new
            {
                userId = memberData.id,
                newRank = "Officer" // Promote to Officer
            };
            
            var response = await ApiClient.PostAsync<object, MemberActionResponse>($"/clans/{clanData.id}/promote", requestData);
            
            if (response?.success == true)
            {
                ShowMemberStatus("Member promoted successfully!", false);
                NotifyActionCompleted();
            }
            else
            {
                ShowMemberStatus(response?.message ?? "Failed to promote member", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                403 => "You don't have permission to promote members",
                404 => "Member not found",
                _ => "Failed to promote member"
            };
            
            ShowMemberStatus(errorMessage, true);
            Debug.LogError($"Promote member error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowMemberStatus("Network error. Please try again.", true);
            Debug.LogError($"Promote member error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(promoteButton, true);
        }
    }
    
    private async Task DemoteMemberAsync()
    {
        if (memberData == null || clanData == null) return;
        
        try
        {
            ShowMemberStatus("Demoting member...", false);
            SetButtonInteractable(demoteButton, false);
            
            var requestData = new
            {
                userId = memberData.id,
                newRank = "Member" // Demote to Member
            };
            
            var response = await ApiClient.PostAsync<object, MemberActionResponse>($"/clans/{clanData.id}/promote", requestData);
            
            if (response?.success == true)
            {
                ShowMemberStatus("Member demoted successfully!", false);
                NotifyActionCompleted();
            }
            else
            {
                ShowMemberStatus(response?.message ?? "Failed to demote member", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                403 => "You don't have permission to demote members",
                404 => "Member not found",
                _ => "Failed to demote member"
            };
            
            ShowMemberStatus(errorMessage, true);
            Debug.LogError($"Demote member error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowMemberStatus("Network error. Please try again.", true);
            Debug.LogError($"Demote member error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(demoteButton, true);
        }
    }
    
    private async Task KickMemberAsync()
    {
        if (memberData == null || clanData == null) return;
        
        try
        {
            ShowMemberStatus("Kicking member...", false);
            SetButtonInteractable(kickButton, false);
            
            var requestData = new
            {
                userId = memberData.id
            };
            
            var response = await ApiClient.PostAsync<object, MemberActionResponse>($"/clans/{clanData.id}/kick", requestData);
            
            if (response?.success == true)
            {
                ShowMemberStatus("Member kicked successfully!", false);
                NotifyActionCompleted();
            }
            else
            {
                ShowMemberStatus(response?.message ?? "Failed to kick member", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                403 => "You don't have permission to kick members",
                404 => "Member not found",
                _ => "Failed to kick member"
            };
            
            ShowMemberStatus(errorMessage, true);
            Debug.LogError($"Kick member error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowMemberStatus("Network error. Please try again.", true);
            Debug.LogError($"Kick member error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(kickButton, true);
        }
    }
    
    private async Task DonateToDonationAmountAsync()
    {
        if (memberData == null || donationAmountInput == null) return;
        
        string amountStr = donationAmountInput.text.Trim();
        if (!int.TryParse(amountStr, out int amount) || amount < minDonationAmount || amount > maxDonationAmount)
        {
            ShowMemberStatus($"Please enter a valid amount between {minDonationAmount} and {maxDonationAmount}", true);
            return;
        }
        
        try
        {
            ShowMemberStatus("Donating carrots...", false);
            SetButtonInteractable(donateButton, false);
            
            var requestData = new
            {
                targetUserId = memberData.id,
                amount = amount
            };
            
            // Note: This endpoint would need to be created in the backend
            var response = await ApiClient.PostAsync<object, DonationResponse>($"/user/donate", requestData);
            
            if (response?.success == true)
            {
                ShowMemberStatus($"Donated {amount} carrots successfully!", false);
                
                // Clear the input field
                donationAmountInput.text = "";
            }
            else
            {
                ShowMemberStatus(response?.message ?? "Failed to donate carrots", true);
            }
        }
        catch (ApiException ex)
        {
            string errorMessage = ex.StatusCode switch
            {
                400 => "Invalid donation amount or insufficient funds",
                403 => "You cannot donate to this user",
                404 => "User not found",
                _ => "Failed to donate carrots"
            };
            
            ShowMemberStatus(errorMessage, true);
            Debug.LogError($"Donate carrots error: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowMemberStatus("Network error. Please try again.", true);
            Debug.LogError($"Donate carrots error: {ex.Message}");
        }
        finally
        {
            SetButtonInteractable(donateButton, true);
        }
    }
    
    #endregion
    
    #region Input Validation
    
    private void ValidateDonationInput(string value)
    {
        if (donateButton == null) return;
        
        bool isValid = int.TryParse(value.Trim(), out int amount) && 
                      amount >= minDonationAmount && 
                      amount <= maxDonationAmount;
        
        donateButton.interactable = isValid;
    }
    
    #endregion
    
    #region UI Utilities
    
    private void ShowMemberStatus(string message, bool isError)
    {
        // Show status in parent modal if available
        if (parentModal != null)
        {
            parentModal.ShowPublicStatus(message, isError);
        }
        else
        {
            Debug.Log($"[ClanMemberItem] {memberData?.username}: {message}");
        }
    }
    
    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
    
    private void NotifyActionCompleted()
    {
        OnMemberActionCompleted?.Invoke(this);
    }
    
    #endregion
}

#region Data Structures

[Serializable]
public class MemberActionResponse
{
    public bool success;
    public string message;
    public object promotedUser; // Contains user info after action
    public object clan; // Contains updated clan info
}

[Serializable]
public class DonationResponse
{
    public bool success;
    public string message;
    public int newBalance; // Donor's new balance
    public int recipientNewBalance; // Recipient's new balance
}

#endregion