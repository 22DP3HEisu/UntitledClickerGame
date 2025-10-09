using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
        SceneManager.LoadScene("game");
    }
}