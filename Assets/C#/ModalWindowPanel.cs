using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;

public class ModalWindowPanel: MonoBehaviour
{
    [Header("Header")] 
    [SerializeField]
    private Transform headerArea;
    [SerializeField]
    private TextMeshProUGUI titleField;

    [Header("Content")]
    [SerializeField]
    private Transform _contentArea;
    [SerializeField]
    private Transform _verticalLayoutArea;
    [SerializeField]
    private Image _heroImage;
    [SerializeField]
    private TextMeshProUGUI _heroText; 
    [Space()]
    [SerializeField]
    private Transform _horizontalLayoutArea;
    [SerializeField]
    private Transform _iconContainer;
    [SerializeField]
    private Image _iconImage;
    [SerializeField]
    private TextMeshProUGUI _iconText;

    [Header("Footer")]
    [SerializeField]
    private Transform _footerArea;
    [SerializeField]
    private Button _confirmButton;
    [SerializeField] 
    private TextMeshProUGUI _confirmText;
    [SerializeField] 
    private string confirmMessage;
    [Space()]

    [SerializeField]
    private Button _declineButton;
    [SerializeField] 
    private TextMeshProUGUI _declineText;
    [SerializeField] 
    private string declineMessage;
    [Space()]
    
    [SerializeField]
    private Button _alternateButton;
    [SerializeField] 
    private TextMeshProUGUI _alternateText;
    [SerializeField] 
    private string alternateMessage;

    private Action onConfirmCallback;
    private Action onDeclineCallback;
    private Action onAlternateCallback;

    private Action onConfirmAction;
    private Action onDeclineAction;
    private Action onAlternateAction;

    public void Confirm(int addCarrots, string confirmMessage)
    {
        _confirmText.text = confirmMessage;
        CurrencySyncManager.Instance.AddCurrency(addCarrots);
        Close();
    }
    public void Decline(int removeCarrots, string declineMessage)
    {
        _declineText.text = declineMessage;
        CurrencySyncManager.Instance.SpendCurrency(removeCarrots);
        Close();
    }
    public void Alternate()
    {
        onAlternateAction?.Invoke();
        Close();
    }

    public void ShowAsHero(
        string title,
        Sprite imageToShow,
        string message,
        string confirmMessage,
        string declineMessage,
        string alternateMessage,
        Action confirmAction,
        Action declineAction = null,
        Action alternateAction = null,
        int addCarrots = 0,
        int removeCarrots = 0,
        string confirmButtonText = "Confirm",
        string declineButtonText = "Decline",
        string alternateButtonText = "Alternate"
        )
    {
        LeanTween.cancel(gameObject);

        _horizontalLayoutArea.gameObject.SetActive(false);
        _verticalLayoutArea.gameObject.SetActive(true);

        // Hide the header if there's no title
        bool hasTitle = !string.IsNullOrEmpty(title);
        headerArea.gameObject.SetActive(hasTitle);
        titleField.text = title;

        _heroImage.sprite = imageToShow;
        _heroText.text = message;

        // Set button texts here
        _confirmText.text = confirmButtonText;
        _declineText.text = declineButtonText;
        _alternateText.text = alternateButtonText;

        _confirmButton.onClick.RemoveAllListeners();
        _declineButton.onClick.RemoveAllListeners();
        _alternateButton.onClick.RemoveAllListeners();

        _confirmButton.onClick.AddListener(() => Confirm(addCarrots, confirmMessage));
        _declineButton.onClick.AddListener(() => Decline(removeCarrots, declineMessage));
        _alternateButton.onClick.AddListener(Alternate);

        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }


    
}