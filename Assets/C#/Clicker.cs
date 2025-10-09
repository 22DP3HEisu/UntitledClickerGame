using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Clicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button clickButton;
    [SerializeField] public Transform targetTransform;
    [SerializeField] public Transform HorseSprite;
    [SerializeField] public ClickPopupSpawner popupSpawner;

    [Header("Animation")]
    [SerializeField] private float scaleUpFactor = 1.2f;
    [SerializeField] private float scaleDuration = 0.1f;

    private Vector3 originalScale;
    private Coroutine resetCoroutine;

    void Start()
    {
        if (HorseSprite != null)
            originalScale = HorseSprite.localScale;
        else if (targetTransform != null)
            originalScale = targetTransform.localScale;
        else
            originalScale = transform.localScale;

        if (clickButton != null)
            clickButton.onClick.AddListener(OnClick);
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
        if (HorseSprite != null)
        {
            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            HorseSprite.localScale = originalScale * scaleUpFactor;
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

        if (ClickManager.Instance != null)
            ClickManager.Instance.AddClicks(clickValue);

        // Track click for achievements
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnPlayerClick();

        Vector2 popupPos = Vector2.zero;
        if (targetTransform != null)
            popupPos = (Vector2)targetTransform.position;
        else if (HorseSprite != null)
            popupPos = (Vector2)HorseSprite.position;
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
        if (HorseSprite != null)
            HorseSprite.localScale = originalScale;
        else if (targetTransform != null)
            targetTransform.localScale = originalScale;
        resetCoroutine = null;
    }
}