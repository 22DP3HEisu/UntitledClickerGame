using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Manages clan chat functionality including message display, sending, and auto-refresh
/// </summary>
public class ClanChatManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messageItemPrefab;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button refreshButton;
    
    [Header("Chat Settings")]
    [SerializeField] private int maxMessages = 100;
    [SerializeField] private float autoRefreshInterval = 5f; // seconds
    [SerializeField] private bool enableAutoRefresh = true;
    
    [Header("Message Colors")]
    [SerializeField] private Color chatMessageColor = Color.white;
    [SerializeField] private Color systemMessageColor = Color.yellow;
    [SerializeField] private Color joinLeaveMessageColor = Color.green;
    [SerializeField] private Color leaderMessageColor = Color.gold;
    
    // State
    private int currentClanId = -1;
    private List<ChatMessage> messages = new List<ChatMessage>();
    private float lastRefreshTime;
    private bool isRefreshing = false;
    
    // Events
    public event Action<ChatMessage> OnNewMessage;
    public event Action<string> OnChatError;
    
    #region Unity Lifecycle
    
    private void Start()
    {
        SetupUI();
    }
    
    private void Update()
    {
        // Auto-refresh chat if enabled and interval has passed
        if (enableAutoRefresh && currentClanId > 0 && !isRefreshing)
        {
            if (Time.time - lastRefreshTime >= autoRefreshInterval)
            {
                _ = RefreshChatAsync();
            }
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Initialize chat for a specific clan
    /// </summary>
    public async void InitializeChatForClan(int clanId)
    {
        if (clanId <= 0)
        {
            Debug.LogError("Invalid clan ID provided to chat manager");
            return;
        }
        
        currentClanId = clanId;
        messages.Clear();
        ClearMessageDisplay();
        
        await LoadChatMessagesAsync();
    }
    
    /// <summary>
    /// Send a message to the current clan chat
    /// </summary>
    public async void SendMessage()
    {
        await SendMessageAsync();
    }
    
    /// <summary>
    /// Manually refresh chat messages
    /// </summary>
    public async void RefreshChat()
    {
        await RefreshChatAsync();
    }
    
    /// <summary>
    /// Close chat (cleanup)
    /// </summary>
    public void CloseChat()
    {
        currentClanId = -1;
        messages.Clear();
        ClearMessageDisplay();
    }
    
    #endregion
    
    #region UI Setup
    
    private void SetupUI()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(() => _ = SendMessageAsync());
        }
        
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(() => _ = RefreshChatAsync());
        }
        
        if (messageInput != null)
        {
            messageInput.onSubmit.RemoveAllListeners();
            messageInput.onSubmit.AddListener((text) => { _ = SendMessageAsync(); });
        }
    }
    
    #endregion
    
    #region Chat Operations
    
    private async Task LoadChatMessagesAsync()
    {
        if (currentClanId <= 0) return;
        
        try
        {
            isRefreshing = true;
            
            var response = await ApiClient.GetAsync<ChatResponse>($"/chat/clan/{currentClanId}?limit={maxMessages}&offset=0");
            
            if (response?.success == true && response.messages != null)
            {
                messages.Clear();
                messages.AddRange(response.messages);
                UpdateMessageDisplay();
                ScrollToBottom();
                
                Debug.Log($"Loaded {messages.Count} chat messages for clan {currentClanId}");
            }
            else
            {
                string errorMsg = response?.message ?? "Failed to load chat messages";
                Debug.LogWarning($"Chat load failed: {errorMsg}");
                OnChatError?.Invoke(errorMsg);
            }
        }
        catch (ApiException ex)
        {
            string errorMsg = ex.StatusCode switch
            {
                403 => "You don't have permission to view this clan's chat",
                404 => "Clan not found",
                _ => "Failed to load chat messages"
            };
            
            Debug.LogError($"Chat load error: {ex.StatusCode} - {ex.Message}");
            OnChatError?.Invoke(errorMsg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Chat load error: {ex.Message}");
            OnChatError?.Invoke("Failed to load chat messages");
        }
        finally
        {
            isRefreshing = false;
            lastRefreshTime = Time.time;
        }
    }
    
    private async Task SendMessageAsync()
    {
        if (currentClanId <= 0 || messageInput == null) return;
        
        string messageText = messageInput.text?.Trim();
        if (string.IsNullOrEmpty(messageText)) return;
        
        try
        {
            sendButton.interactable = false;
            
            var requestData = new { message = messageText, messageType = "chat" };
            var response = await ApiClient.PostAsync<object, SendMessageResponse>($"/chat/clan/{currentClanId}", requestData);
            
            if (response?.success == true && response.chatMessage != null)
            {
                // Add the new message to our local list
                messages.Add(response.chatMessage);
                
                // Keep only the most recent messages
                if (messages.Count > maxMessages)
                {
                    messages = messages.Skip(messages.Count - maxMessages).ToList();
                }
                
                UpdateMessageDisplay();
                ScrollToBottom();
                
                // Clear input
                messageInput.text = "";
                messageInput.ActivateInputField();
                
                // Trigger event
                OnNewMessage?.Invoke(response.chatMessage);
                
                Debug.Log($"Message sent successfully: {messageText}");
            }
            else
            {
                string errorMsg = response?.message ?? "Failed to send message";
                Debug.LogWarning($"Send message failed: {errorMsg}");
                OnChatError?.Invoke(errorMsg);
            }
        }
        catch (ApiException ex)
        {
            string errorMsg = ex.StatusCode switch
            {
                403 => "You don't have permission to send messages to this clan",
                429 => "You're sending messages too quickly. Please wait a moment",
                400 => "Message is too long or invalid",
                _ => "Failed to send message"
            };
            
            Debug.LogError($"Send message error: {ex.StatusCode} - {ex.Message}");
            OnChatError?.Invoke(errorMsg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send message error: {ex.Message}");
            OnChatError?.Invoke("Failed to send message");
        }
        finally
        {
            sendButton.interactable = true;
        }
    }
    
    private async Task RefreshChatAsync()
    {
        if (currentClanId <= 0 || isRefreshing) return;
        
        int lastMessageCount = messages.Count;
        await LoadChatMessagesAsync();
        
        // Check if there are new messages
        if (messages.Count > lastMessageCount)
        {
            int newMessageCount = messages.Count - lastMessageCount;
            Debug.Log($"Received {newMessageCount} new messages");
        }
    }
    
    #endregion
    
    #region UI Display
    
    private void UpdateMessageDisplay()
    {
        ClearMessageDisplay();
        
        foreach (var message in messages)
        {
            CreateMessageItem(message);
        }
    }
    
    private void ClearMessageDisplay()
    {
        if (messageContainer == null) return;
        
        foreach (Transform child in messageContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    private void CreateMessageItem(ChatMessage message)
    {
        if (messageItemPrefab == null || messageContainer == null) return;
        
        var messageItem = Instantiate(messageItemPrefab, messageContainer);
        var chatMessageComponent = messageItem.GetComponent<ChatMessageItem>();
        
        if (chatMessageComponent != null)
        {
            chatMessageComponent.SetupMessage(message, GetMessageColor(message));
        }
        else
        {
            // Fallback: use basic text component
            var textComponent = messageItem.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                string timestamp = DateTime.Parse(message.timestamp).ToString("HH:mm");
                string userPrefix = message.user.isLeader ? "[Leader]" : 
                                  message.user.rank == "Officer" ? "[Officer]" : "";
                
                textComponent.text = $"[{timestamp}] {userPrefix}{message.user.username}: {message.message}";
                textComponent.color = GetMessageColor(message);
            }
        }
    }
    
    private Color GetMessageColor(ChatMessage message)
    {
        return message.messageType switch
        {
            "system" => systemMessageColor,
            "join" or "leave" => joinLeaveMessageColor,
            "chat" when message.user.isLeader => leaderMessageColor,
            _ => chatMessageColor
        };
    }
    
    private void ScrollToBottom()
    {
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    #endregion
}

#region Data Structures

[Serializable]
public class ChatResponse
{
    public bool success;
    public string message;
    public ChatMessage[] messages;
    public ChatPagination pagination;
}

[Serializable]
public class SendMessageResponse
{
    public bool success;
    public string message;
    public ChatMessage chatMessage;
}

[Serializable]
public class ChatMessage
{
    public int id;
    public string message;
    public string messageType;
    public string timestamp;
    public ChatUser user;
}

[Serializable]
public class ChatUser
{
    public string username;
    public string rank;
    public bool isLeader;
}

[Serializable]
public class ChatPagination
{
    public int limit;
    public int offset;
    public int total;
}

#endregion