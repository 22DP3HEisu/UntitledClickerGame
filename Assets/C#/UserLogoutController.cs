using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UserLogoutController : MonoBehaviour
{
    [Header("Assign the logout button here")]
    [SerializeField] private Button logoutButton;
    private void Start()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        else
        {
            Debug.LogWarning("Logout button is not assigned in the inspector.");
        }
    }

    private void OnLogoutClicked()
    {
        UserManager.ForceLogout();
    }
}