using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroButtonManager : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        // Set up the login button click listener
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        else
        {
            Debug.LogWarning("Login button is not assigned in the IntroButtonManager!");
        }
        
        // Set up the register button click listener
        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        }
        else
        {
            Debug.LogWarning("Register button is not assigned in the IntroButtonManager!");
        }
    }

    // Handle login button click
    private void OnLoginButtonClicked()
    {
        Debug.Log("Login button clicked!");
        
        SceneManager.LoadScene("Login");
    }
    
    // Handle register button click
    private void OnRegisterButtonClicked()
    {
        Debug.Log("Register button clicked!");

        SceneManager.LoadScene("Register");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Clean up event listeners when the object is destroyed
    void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        }
        
        if (registerButton != null)
        {
            registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        }
    }
}
