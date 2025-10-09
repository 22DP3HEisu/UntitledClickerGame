using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GoToAchievments : MonoBehaviour
{
    [Header("UI")]
    public Button Achievments;

    [Header("Scene")]
    [Tooltip("Name of the achievements scene as in Build Settings")]
    [SerializeField] private string achievementsSceneName = "Achievements";

    [Tooltip("Load achievements additively so the current game scene remains loaded and its scripts keep running")]
    [SerializeField] private bool loadAdditively = true;

    [Header("Behavior")]
    [Tooltip("If true, Time.timeScale will be set to 0 when achievements are open and restored on close.")]
    [SerializeField] private bool pauseGameWhenOpen = false;

    [Header("Navigation hide (optional)")]
    [Tooltip("Assign the Transform of the navigation tab (or root) that should be hidden while achievements are open.")]
    [SerializeField] private Transform navigationRoot;

    public enum NavigationHideMode { Scale, BlockRaycasts, DisableGameObject }

    [Tooltip("How to hide the navigationRoot while achievements are open. BlockRaycasts prevents clicks without disabling objects.")]
    [SerializeField] private NavigationHideMode navigationHideMode = NavigationHideMode.BlockRaycasts;

    [Header("Buttons to disable when opening Achievements")]
    [Tooltip("Example: assign the Register button here so it is disabled while Achievements are open.")]
    [SerializeField] private Button registerButton;
    [Tooltip("Second button to disable while Achievements are open.")]
    [SerializeField] private Button secondButton;

    private AsyncOperation loadOp;
    private bool isLoaded;
    private Scene originalScene;
    private Scene loadedScene;

    // Saved UI state so we can restore it
    private readonly List<Canvas> disabledCanvases = new List<Canvas>();
    private readonly List<GraphicRaycaster> disabledRaycasters = new List<GraphicRaycaster>();
    private readonly List<EventSystem> disabledEventSystems = new List<EventSystem>();

    // Navigation root original transform cache
    private Vector3 navOriginalScale = Vector3.one;
    private Vector3 navOriginalLocalPosition = Vector3.zero;
    private bool navSaved = false;

    // CanvasGroup used for raycast blocking (if chosen)
    private CanvasGroup navCanvasGroup;
    private bool navCreatedCanvasGroup = false;
    private bool navOriginalBlocks = true;
    private bool navOriginalInteractable = true;
    private bool navWasActive = true;

    // Saved states for the two buttons
    private bool registerSaved = false;
    private bool registerWasActive;
    private bool registerOriginalInteractable;

    private bool secondSaved = false;
    private bool secondWasActive;
    private bool secondOriginalInteractable;

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
        if (string.IsNullOrEmpty(achievementsSceneName))
        {
            Debug.LogWarning("Achievements scene name is empty. Set the scene name in the inspector.");
            yield break;
        }

        Achievments.interactable = false;

        // Save current active scene so we can restore it later
        originalScene = SceneManager.GetActiveScene();

        var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        loadOp = SceneManager.LoadSceneAsync(achievementsSceneName, mode);
        if (loadOp == null)
        {
            Debug.LogError($"Failed to start loading scene '{achievementsSceneName}'. Make sure it is added to Build Settings.");
            Achievments.interactable = true;
            yield break;
        }

        while (!loadOp.isDone)
            yield return null;

        // Get loaded scene reference and make it active
        loadedScene = SceneManager.GetSceneByName(achievementsSceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }
        else
        {
            Debug.LogWarning($"Loaded scene '{achievementsSceneName}' not found by name after load.");
        }

        // If a specific navigation Transform is assigned, hide only that (without disabling unrelated objects).
        // Otherwise fall back to hiding UI across the project (previous behavior).
        if (loadAdditively)
        {
            if (navigationRoot != null)
                HideNavigationRoot();
            else
                HideUIExceptScene(loadedScene);

            // Disable the two specified buttons (if assigned)
            DisableListedButtons();
        }

        isLoaded = true;
        Achievments.interactable = true;
        if (pauseGameWhenOpen) Time.timeScale = 0f;

        Debug.Log($"Loaded achievements scene '{achievementsSceneName}' (additive={loadAdditively}). Original scene kept loaded and its scripts continue running.");
    }

    private IEnumerator UnloadAchievementsAsync()
    {
        if (!isLoaded) yield break;
        Achievments.interactable = false;

        // Restore UI before unloading achievements scene
        if (loadAdditively)
        {
            // Restore the two buttons first
            RestoreListedButtons();

            if (navigationRoot != null)
                RestoreNavigationRoot();
            else
                RestoreOriginalUI();

            // Restore original active scene if still loaded
            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalScene);
            }
        }

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
        if (pauseGameWhenOpen) Time.timeScale = 1f;
        Debug.Log($"Unloaded achievements scene '{achievementsSceneName}'. Original scene restored as active (if it was kept).");
    }

    // Disable every UI component that is not in the keepScene (covers DontDestroyOnLoad objects too)
    private void HideUIExceptScene(Scene keepScene)
    {
        disabledCanvases.Clear();
        disabledRaycasters.Clear();
        disabledEventSystems.Clear();

        // Disable all Canvas components that are not in the achievements scene and not protected
        var allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in allCanvases)
        {
            if (c == null) continue;
            var cScene = c.gameObject.scene;
            // If canvas belongs to the achievements scene, leave it alone
            if (keepScene.IsValid() && cScene == keepScene) continue;
            // If canvas is under a protected transform, skip disabling
            // (existing protected logic remains in case you use it elsewhere)
            // You can keep protected lists empty; registerButton/secondButton are handled separately.
            if (IsExcluded(c.gameObject)) continue;

            if (c.enabled)
            {
                c.enabled = false;
                disabledCanvases.Add(c);
            }
        }

        // Disable GraphicRaycasters not in the achievements scene and not protected
        var allRaycasters = FindObjectsOfType<GraphicRaycaster>(true);
        foreach (var r in allRaycasters)
        {
            if (r == null) continue;
            var rScene = r.gameObject.scene;
            if (keepScene.IsValid() && rScene == keepScene) continue;
            if (IsExcluded(r.gameObject)) continue;

            if (r.enabled)
            {
                r.enabled = false;
                disabledRaycasters.Add(r);
            }
        }

        // Disable EventSystems not in the achievements scene and not protected
        var allEventSystems = FindObjectsOfType<EventSystem>(true);
        foreach (var es in allEventSystems)
        {
            if (es == null) continue;
            var esScene = es.gameObject.scene;
            if (keepScene.IsValid() && esScene == keepScene) continue;
            if (IsExcluded(es.gameObject)) continue;

            if (es.enabled)
            {
                es.enabled = false;
                disabledEventSystems.Add(es);
            }
        }
    }

    // Restore UI components we disabled
    private void RestoreOriginalUI()
    {
        foreach (var c in disabledCanvases)
        {
            if (c != null) c.enabled = true;
        }
        disabledCanvases.Clear();

        foreach (var r in disabledRaycasters)
        {
            if (r != null) r.enabled = true;
        }
        disabledRaycasters.Clear();

        foreach (var es in disabledEventSystems)
        {
            if (es != null) es.enabled = true;
        }
        disabledEventSystems.Clear();
    }

    // Returns true if the given GameObject is a child of any transform in protected groups
    private bool IsExcluded(GameObject go)
    {
        if (go == null) return false;

        // existing protected arrays left for backward compatibility (you do not need to use them)
        return false;
    }

    // Disable the two specific buttons (cache their original state)
    private void DisableListedButtons()
    {
        if (registerButton != null && !registerSaved)
        {
            registerWasActive = registerButton.gameObject.activeSelf;
            registerOriginalInteractable = registerButton.interactable;
            registerSaved = true;

            // fully hide / disable the button so it cannot be clicked or seen
            registerButton.interactable = false;
            registerButton.gameObject.SetActive(false);
        }

        if (secondButton != null && !secondSaved)
        {
            secondWasActive = secondButton.gameObject.activeSelf;
            secondOriginalInteractable = secondButton.interactable;
            secondSaved = true;

            secondButton.interactable = false;
            secondButton.gameObject.SetActive(false);
        }
    }

    // Restore the two specific buttons to their original states
    private void RestoreListedButtons()
    {
        if (registerButton != null && registerSaved)
        {
            registerButton.gameObject.SetActive(registerWasActive);
            registerButton.interactable = registerOriginalInteractable;
            registerSaved = false;
        }

        if (secondButton != null && secondSaved)
        {
            secondButton.gameObject.SetActive(secondWasActive);
            secondButton.interactable = secondOriginalInteractable;
            secondSaved = false;
        }
    }

    // Hide navigationRoot by chosen mode.
    private void HideNavigationRoot()
    {
        if (navigationRoot == null) return;

        if (!navSaved)
        {
            navOriginalScale = navigationRoot.localScale;
            navOriginalLocalPosition = navigationRoot.localPosition;
            navWasActive = navigationRoot.gameObject.activeSelf;
            navSaved = true;
        }

        switch (navigationHideMode)
        {
            case NavigationHideMode.Scale:
                navigationRoot.localScale = Vector3.zero;
                break;

            case NavigationHideMode.DisableGameObject:
                navigationRoot.gameObject.SetActive(false);
                break;

            case NavigationHideMode.BlockRaycasts:
                // Use or add a CanvasGroup on the navigationRoot to block raycasts without disabling components.
                navCanvasGroup = navigationRoot.GetComponent<CanvasGroup>();
                if (navCanvasGroup == null)
                {
                    // Try to find in children before adding
                    navCanvasGroup = navigationRoot.GetComponentInChildren<CanvasGroup>();
                }

                if (navCanvasGroup == null)
                {
                    navCanvasGroup = navigationRoot.gameObject.AddComponent<CanvasGroup>();
                    navCreatedCanvasGroup = true;
                }
                // Save original values (only if not saved yet)
                navOriginalBlocks = navCanvasGroup.blocksRaycasts;
                navOriginalInteractable = navCanvasGroup.interactable;

                // Hide visually via scale (keeps structure) and block input
                navigationRoot.localScale = Vector3.zero;
                navCanvasGroup.interactable = false;
                navCanvasGroup.blocksRaycasts = false;
                break;
        }
    }

    private void RestoreNavigationRoot()
    {
        if (navigationRoot == null || !navSaved) return;

        switch (navigationHideMode)
        {
            case NavigationHideMode.Scale:
                navigationRoot.localScale = navOriginalScale;
                break;

            case NavigationHideMode.DisableGameObject:
                // restore active state
                navigationRoot.gameObject.SetActive(navWasActive);
                break;

            case NavigationHideMode.BlockRaycasts:
                // restore visual
                navigationRoot.localScale = navOriginalScale;

                if (navCanvasGroup != null)
                {
                    navCanvasGroup.blocksRaycasts = navOriginalBlocks;
                    navCanvasGroup.interactable = navOriginalInteractable;
                    if (navCreatedCanvasGroup)
                    {
                        // remove the temporary CanvasGroup we added
                        Destroy(navCanvasGroup);
                        navCanvasGroup = null;
                        navCreatedCanvasGroup = false;
                    }
                }
                break;
        }

        // restore local position if you changed it previously
        navigationRoot.localPosition = navOriginalLocalPosition;
        navSaved = false;
    }
}