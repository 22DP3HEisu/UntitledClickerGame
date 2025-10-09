using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GoToAchievments : MonoBehaviour
{
    [Header("UI")]
    public Button Achievments;

    [Header("Scene")]
    [Tooltip("Name of the achievements scene as in Build Settings")]
    [SerializeField] private string achievementsSceneName = "Achievements";

    [Tooltip("If true, the achievements scene will be loaded additively so the current game scene remains loaded")]
    [SerializeField] private bool loadAdditively = true;

    private AsyncOperation loadOp;
    private bool isLoaded;

    void Start()
    {
        if (Achievments != null)
            Achievments.onClick.AddListener(OnToggleAchievements);
    }

    void OnDestroy()
    {
        if (Achievments != null)
            Achievments.onClick.RemoveListener(OnToggleAchievements);
    }

    private void OnToggleAchievements()
    {
        if (string.IsNullOrEmpty(achievementsSceneName))
        {
            Debug.LogWarning("Achievements scene name is empty. Set the scene name in the inspector.");
            return;
        }

        if (!isLoaded)
            StartCoroutine(LoadAchievementsAsync());
        else
            StartCoroutine(UnloadAchievementsAsync());
    }

    public void OpenAchievements() => StartCoroutine(LoadAchievementsAsync());
    public void CloseAchievements() => StartCoroutine(UnloadAchievementsAsync());

    private IEnumerator LoadAchievementsAsync()
    {
        if (isLoaded) yield break;
        Achievments.interactable = false;

        var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        loadOp = SceneManager.LoadSceneAsync(achievementsSceneName, mode);
        if (loadOp == null)
        {
            Debug.LogError($"Failed to start loading scene '{achievementsSceneName}'. Make sure it is added to Build Settings.");
            Achievments.interactable = true;
            yield break;
        }

        // Wait until the scene is loaded
        while (!loadOp.isDone)
            yield return null;

        isLoaded = true;
        Achievments.interactable = true;
        Debug.Log($"Loaded achievements scene '{achievementsSceneName}' (additive={loadAdditively}).");
    }

    private IEnumerator UnloadAchievementsAsync()
    {
        if (!isLoaded) yield break;
        Achievments.interactable = false;

        var unloadOp = SceneManager.UnloadSceneAsync(achievementsSceneName);
        if (unloadOp == null)
        {
            Debug.LogError($"Failed to start unloading scene '{achievementsSceneName}'.");
            Achievments.interactable = true;
            yield break;
        }

        while (!unloadOp.isDone)
            yield return null;

        isLoaded = false;
        Achievments.interactable = true;
        Debug.Log($"Unloaded achievements scene '{achievementsSceneName}'.");
    }
}