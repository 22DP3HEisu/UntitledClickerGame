using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Clicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button clickButton;
    [SerializeField] public Transform targetTransform;
    [SerializeField] public Transform Sprite;
    [SerializeField] public ClickPopupSpawner popupSpawner;

    [Header("Animation")]
    [SerializeField] private float scaleUpFactor = 1.2f;
    [SerializeField] private float scaleDuration = 0.1f;

    private Vector3 originalScale;
    private Coroutine resetCoroutine;

    void Start()
    {
        if (Sprite != null)
            originalScale = Sprite.localScale;
        else if (targetTransform != null)
            originalScale = targetTransform.localScale;
        else
            originalScale = transform.localScale;
    }

    void OnEnable()
    {
        if (clickButton != null)
            clickButton.onClick.AddListener(OnClick);
    }

    void OnDisable()
    {
        if (clickButton != null)
            clickButton.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (Sprite != null)
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            Sprite.localScale = originalScale * scaleUpFactor;
            resetCoroutine = StartCoroutine(ResetScale());
        }
        else if (targetTransform != null)
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            targetTransform.localScale = originalScale * scaleUpFactor;
            resetCoroutine = StartCoroutine(ResetScale());
        }

        // Apply click income boost from achievements
        int clickValue = 1;
        if (AchievementManager.Instance != null)
        {
            clickValue = Mathf.RoundToInt(clickValue * AchievementManager.Instance.GetClickIncomeBoost());
        }

        // Add carrots to currency manager
        if (CurrencySyncManager.Instance != null)
        {
            CurrencySyncManager.Instance.AddCurrency(clickValue);
        }

        // Track click for achievements
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnPlayerClick();

        Vector2 popupPos = Vector2.zero;
        if (targetTransform != null)
            popupPos = (Vector2)targetTransform.position;
        else if (Sprite != null)
            popupPos = (Vector2)Sprite.position;
        else if (clickButton != null)
        {
            var cam = Camera.main;
            if (cam != null)
                popupPos = cam.ScreenToWorldPoint(clickButton.transform.position);
        }

        if (popupSpawner != null)
            popupSpawner.SpawnPopup(popupPos, $"+{clickValue}");
    }

    private IEnumerator ResetScale()
    {
        yield return new WaitForSeconds(scaleDuration);
        if (Sprite != null)
            Sprite.localScale = originalScale;
        else if (targetTransform != null)
            targetTransform.localScale = originalScale;
        resetCoroutine = null;
    }
}