using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SettingsPopUp : MonoBehaviour
{
    [SerializeField] private Button Achievments;
    [SerializeField] private Sprite Quests;
    [SerializeField] private Image Settings;
    [SerializeField] private Transform SettingsPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SettingsPanel.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
