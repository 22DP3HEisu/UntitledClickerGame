using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;

public class BackToGame : MonoBehaviour
{
    [SerializeField] private Button backToGameButton;
    [Tooltip("Optional — GraphicRaycaster to use for hit testing. If null, will try to auto-find on the button's Canvas.")]
    [SerializeField] private GraphicRaycaster graphicRaycaster;

    private void Start()
    {
        if (backToGameButton != null)
        {
            backToGameButton.onClick.AddListener(LoadGameScene);
        }

        if (backToGameButton == null)
        {
            Debug.LogWarning("🔘 BackToGame: backToGameButton is not assigned.");
            return;
        }

        // Auto-find GraphicRaycaster if not assigned
        if (graphicRaycaster == null)
        {
            var canvas = backToGameButton.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            }
        }

        Debug.Log("🔘 BackToGame: Initialized. Running initial block check.");
        Vector2 screenPos = GetCurrentPointerPosition();
        LogRaycastAtPosition(screenPos);
    }

    private void LoadGameScene()
    {
        Debug.Log("🔘 BackToGame: Button clicked. Checking for blockers before loading scene.");

        if (backToGameButton == null)
        {
            Debug.LogWarning("🔘 BackToGame: backToGameButton is null on click.");
            SceneManager.LoadScene("game");
            return;
        }

        // Log interactable state
        Debug.Log($"🔘 BackToGame: Button.interactable = {backToGameButton.interactable}");

        Vector2 screenPos = GetCurrentPointerPosition();
        LogRaycastAtPosition(screenPos);

        // Do not change behavior — still load the scene as before.
        SceneManager.LoadScene("game");
    }

    // Returns current pointer/touch position in screen coordinates (best-effort).
    private Vector2 GetCurrentPointerPosition()
    {
        // Touch takes priority if present (old Input system)
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;

        // Fallback to mouse
        return Input.mousePosition;
    }

    // Performs a UI raycast at the provided screen position and logs results.
    private void LogRaycastAtPosition(Vector2 screenPos)
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("🔘 BackToGame: EventSystem.current is null — cannot run UI raycast.");
            return;
        }

        // Ensure we have a GraphicRaycaster to query
        if (graphicRaycaster == null)
        {
            Debug.LogWarning("🔘 BackToGame: GraphicRaycaster not found. Assign one or place a GraphicRaycaster on the parent Canvas.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log($"🔘 BackToGame: No UI hits at screenPos {screenPos}.");
            return;
        }

        // Log the entire hit list and indicate whether the button is top-most hit
        Debug.Log($"🔘 BackToGame: UI Raycast hits at screenPos {screenPos}: (count={results.Count})");
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            string hitName = r.gameObject != null ? r.gameObject.name : "(null)";
            Debug.Log($"    [{i}] name='{hitName}', depth={r.depth}, module='{r.module?.GetType().Name}'");
        }

        // Check topmost hit
        var top = results[0];
        if (top.gameObject == backToGameButton.gameObject || top.gameObject.transform.IsChildOf(backToGameButton.transform))
        {
            Debug.Log("🔘 BackToGame: The button is the top-most UI element at the pointer position (not blocked).");
        }
        else
        {
            Debug.LogWarning($"🔘 BackToGame: The button is NOT top-most. Top-most UI object at pointer: '{top.gameObject.name}'. This likely blocks the button.");
        }
    }
}