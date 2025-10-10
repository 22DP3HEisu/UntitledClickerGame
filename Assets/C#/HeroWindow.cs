using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.UI;
using TMPro;


public class ModalWindowTrigger : MonoBehaviour
{
    public float timeRemaining = 120;
    public Text timeText;

    public string title;
    public Sprite sprite;
    public string message;
    public string confirmMessage;
    public string declineMessage;
    public string alternateMessage;
    public int addCarrots;
    public int removeCarrots;

    public UnityEvent onContinueCallBack;
    public UnityEvent onCancelCallBack;
    public UnityEvent onAlternateCallBack;

    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else if (timeRemaining > -1)
        {
            ShowModalWindow();
            timeRemaining = -1;
        }
    }
    
    public void ShowModalWindow()
    {

        Action continueCallback = null; 
        Action cancelCallback = null;
        Action alternateCallback = null;


            continueCallback = () => AddCurrency();

            cancelCallback = () => RemoveCurrency();
        
        if (onAlternateCallBack.GetPersistentEventCount() > 0)
        {
            alternateCallback = () => onAlternateCallBack.Invoke();
        }

        UIController.instance.modalWindow.ShowAsHero(
            title,
            sprite,
            message,
            confirmMessage,
            declineMessage,
            alternateMessage,
            continueCallback,
            cancelCallback,
            alternateCallback,
            addCarrots, 
            removeCarrots,
            confirmMessage,
            declineMessage,
            alternateMessage
        );
    }
    void AddCurrency()
    {
        if (CurrencySyncManager.Instance != null && addCarrots > 0)
        {
            CurrencySyncManager.Instance.AddCurrency(addCarrots);

        }
    }
    void RemoveCurrency()
    {
        if (CurrencySyncManager.Instance != null && removeCarrots > 0)
        {
            CurrencySyncManager.Instance.SpendCurrency(removeCarrots);
        }
    }
}

