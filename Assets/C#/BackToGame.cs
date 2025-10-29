using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;

public class BackToGame : MonoBehaviour
{
    [SerializeField] private Button backToGameButton;

    private void Start()
    {
        if (backToGameButton != null)
        {
            backToGameButton.onClick.AddListener(LoadGameScene);
        }
    }

    private void LoadGameScene()
    {
        Debug.Log("🔘 BackToGame: Button listener added.");
        SceneManager.LoadScene("game");
    }
}