using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GuestChecker : MonoBehaviour
{
    [Header("Redirect Settings")]
    [Tooltip("Name of the scene to load if the user is not authenticated.")]
    [SerializeField] private string registerSceneName = "Register";

    [Tooltip("If true, guests (IsGuest == 1) are allowed to stay.")]
    [SerializeField] private bool allowGuestAccess = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[GuestChecker] No Button component found on this GameObject!");
            return;
        }

        // Add listener to the button click event
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }

    /// <summary>
    /// Called automatically when the button is clicked
    /// </summary>
    private void OnButtonClicked()
    {
        CheckAuthentication();
    }

    private void CheckAuthentication()
    {
        // Skip check if already on Register scene
        if (SceneManager.GetActiveScene().name == registerSceneName)
        {
            Debug.Log("[GuestChecker] On Register scene — skipping auth check.");
            return;
        }

        string authToken = PlayerPrefs.GetString("AuthToken", "");
        int isGuest = PlayerPrefs.GetInt("IsGuest", 0);

        Debug.Log($"[GuestChecker] Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"[GuestChecker] AuthToken: '{authToken}' | IsGuest: {isGuest}");

        bool hasValidToken = !string.IsNullOrEmpty(authToken);
        bool isGuestAllowed = allowGuestAccess && isGuest == 1;

        if (!hasValidToken && !isGuestAllowed)
        {
            Debug.LogWarning("[GuestChecker] No valid token found — redirecting to Register scene...");

            if (Application.CanStreamedLevelBeLoaded(registerSceneName))
            {
                SceneManager.LoadScene(registerSceneName);
            }
            else
            {
                Debug.LogError($"[GuestChecker] Scene '{registerSceneName}' not found in Build Settings!");
            }
        }
        else
        {
            Debug.Log("[GuestChecker] Access granted — valid token or guest allowed.");
        }
    }
}
