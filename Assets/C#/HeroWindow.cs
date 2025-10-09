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

    public UnityEvent onContinueCallBack;
    public UnityEvent onCancelCallBack;
    public UnityEvent onAlternateCallBack;

    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            timeRemaining = 0;
            ShowModalWindow();
        }
    }
    
    public void ShowModalWindow()
    {

        Action continueCallback = null; 
        Action cancelCallback = null;
        Action alternateCallback = null;

        if (onContinueCallBack.GetPersistentEventCount() > 0)
        {
            continueCallback = onContinueCallBack.Invoke;
        }
        if (onCancelCallBack.GetPersistentEventCount() > 0)
        {
            cancelCallback = onCancelCallBack.Invoke;
        }
        if (onAlternateCallBack.GetPersistentEventCount() > 0)
        {
            alternateCallback = onAlternateCallBack.Invoke;
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
            alternateCallback
        );
    }
}

