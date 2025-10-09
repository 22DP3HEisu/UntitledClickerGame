using UnityEngine;
using System;

public class UIController: MonoBehaviour
{
    public static UIController instance;
    [SerializeField]
    private ModalWindowPanel _modalWindow;
    public ModalWindowPanel modalWindow => _modalWindow;
    private void Awake()
    {
        instance = this;
    }
}