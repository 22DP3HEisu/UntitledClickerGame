using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Individual chat message item component
/// </summary>
public class ChatMessageItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text timestampText;
    [SerializeField] private Image rankIcon;
    [SerializeField] private Image backgroundImage;
    
    [Header("Rank Icons (Optional)")]
    [SerializeField] private Sprite leaderIcon;
    [SerializeField] private Sprite officerIcon;
    [SerializeField] private Sprite memberIcon;
    
    [Header("Message Type Colors")]
    [SerializeField] private Color systemMessageBackground = new Color(1f, 1f, 0f, 0.1f);
    [SerializeField] private Color normalMessageBackground = new Color(1f, 1f, 1f, 0.05f);
    
    /// <summary>
    /// Setup the message item with chat data
    /// </summary>
    public void SetupMessage(ChatMessage message, Color textColor)
    {
        if (message == null) return;
        
        // Set username with rank prefix
        if (usernameText != null)
        {
            string rankPrefix = "";
            if (message.user.isLeader)
            {
                rankPrefix = "[Leader] ";
            }
            else if (message.user.rank == "Officer")
            {
                rankPrefix = "[Officer] ";
            }
            
            usernameText.text = rankPrefix + message.user.username;
            usernameText.color = message.user.isLeader ? Color.yellow : Color.white;
        }
        
        // Set message text
        if (messageText != null)
        {
            messageText.text = message.message;
            messageText.color = textColor;
        }
        
        // Set timestamp
        if (timestampText != null)
        {
            if (DateTime.TryParse(message.timestamp, out DateTime dateTime))
            {
                timestampText.text = dateTime.ToString("HH:mm");
            }
            else
            {
                timestampText.text = "";
            }
        }
        
        // Set rank icon
        if (rankIcon != null)
        {
            if (message.user.isLeader && leaderIcon != null)
            {
                rankIcon.sprite = leaderIcon;
                rankIcon.gameObject.SetActive(true);
            }
            else if (message.user.rank == "Officer" && officerIcon != null)
            {
                rankIcon.sprite = officerIcon;
                rankIcon.gameObject.SetActive(true);
            }
            else if (memberIcon != null)
            {
                rankIcon.sprite = memberIcon;
                rankIcon.gameObject.SetActive(true);
            }
            else
            {
                rankIcon.gameObject.SetActive(false);
            }
        }
        
        // Set background color based on message type
        if (backgroundImage != null)
        {
            backgroundImage.color = message.messageType == "system" || 
                                   message.messageType == "join" || 
                                   message.messageType == "leave" 
                                   ? systemMessageBackground 
                                   : normalMessageBackground;
        }
    }
}