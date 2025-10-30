using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroButtonManager : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button playButton;

    private void Start()
    {
        AssignButton(loginButton, OnLoginButtonClicked, "Login");
        AssignButton(registerButton, OnRegisterButtonClicked, "Register");
        AssignButton(playButton, OnPlayButtonClicked, "Play");
    }

    private void AssignButton(Button button, UnityEngine.Events.UnityAction action, string name)
    {
        if (button)
        {
            button.onClick.AddListener(action);
        }
        else
        {
            Debug.LogWarning($"{name} button is not assigned in {nameof(IntroButtonManager)}!");
        }
    }

    private void OnLoginButtonClicked()
    {
        Debug.Log("Login button clicked!");
        SceneManager.LoadScene("Login");
    }

    private void OnRegisterButtonClicked()
    {
        Debug.Log("Register button clicked!");
        SceneManager.LoadScene("Register");
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked! Starting as Guest...");

        string guestUsername = PlayerPrefs.GetString("GuestUsername", "");

        if (string.IsNullOrEmpty(guestUsername))
        {
            guestUsername = $"Guest{Random.Range(1000, 9999)}";
            PlayerPrefs.SetString("GuestUsername", guestUsername);
        }

        PlayerPrefs.SetInt("IsGuest", 1);
        PlayerPrefs.DeleteKey("RegisteredUserId");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.Save();

        Debug.Log($"Starting game as {guestUsername}");
        SceneManager.LoadScene("game");
    }

    private void OnDestroy()
    {
        RemoveButtonListener(loginButton, OnLoginButtonClicked);
        RemoveButtonListener(registerButton, OnRegisterButtonClicked);
        RemoveButtonListener(playButton, OnPlayButtonClicked);
    }

    private void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button)
            button.onClick.RemoveListener(action);
    }
}
