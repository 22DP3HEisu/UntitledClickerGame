using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuestProgressBar : MonoBehaviour
{
    private enum QuestTarget
    {
        TotalCarrots,
        TotalClicks
    }

    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private float tweenDuration = 0.25f;
    [SerializeField] private TMP_Text NextQuestIn;

    [Header("Quest")]
    [SerializeField] private QuestTarget questTarget = QuestTarget.TotalCarrots; // dropdown in inspector
    [SerializeField] private int carrotTarget = 1000;
    [SerializeField] private bool showAsFraction = true;
    [SerializeField] private UnityEvent onQuestCompleted;

    [Tooltip("How many carrots to add to the player's total when this quest completes.")]
    [SerializeField] private int rewardCarrotsOnComplete = 0;

    public event Action OnQuestCompleted; // code hook

    private Coroutine tweenCoroutine;
    private int lastTrackedValue = -1;
    private bool questCompleted = false;

    // Next-quest timer
    private Coroutine nextQuestCoroutine;
    private TimeZoneInfo centralEuropeTimeZone;

    void Awake()
    {
        centralEuropeTimeZone = ResolveCentralEuropeTimeZone();
    }

    void OnEnable()
    {
        if (nextQuestCoroutine == null)
            nextQuestCoroutine = StartCoroutine(NextQuestTimer());
    }

    void OnDisable()
    {
        if (nextQuestCoroutine != null)
        {
            StopCoroutine(nextQuestCoroutine);
            nextQuestCoroutine = null;
        }
    }

    // Set using current and target values
    public void SetProgress(float current, float max)
    {
        float normalized = max > 0f ? current / max : 0f;
        SetProgressNormalized(normalized, current, max);
    }

    // Set using normalized 0..1 value; optional current/max for text formatting
    public void SetProgressNormalized(float normalized, float current = -1f, float max = -1f)
    {
        normalized = Mathf.Clamp01(normalized);

        if (tweenCoroutine != null)
            StopCoroutine(tweenCoroutine);

        tweenCoroutine = StartCoroutine(AnimateFill(fillImage != null ? fillImage.fillAmount : 0f, normalized));

        // update text immediately
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
        }
    }

    // Incremental helper
    public void AddProgress(float addAmount, float current, float max)
    {
        SetProgress(current + addAmount, max);
    }

    private IEnumerator AnimateFill(float from, float to)
    {
        if (fillImage == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < tweenDuration)
        {
            elapsed += Time.deltaTime;
            float t = tweenDuration > 0f ? elapsed / tweenDuration : 1f;
            fillImage.fillAmount = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        fillImage.fillAmount = to;
        tweenCoroutine = null;
    }

    void Update()
    {
        if (questCompleted) return;

        int trackedValue = GetTrackedValue();

        if (trackedValue != lastTrackedValue)
        {
            lastTrackedValue = trackedValue;

            // choose max based on target type
            int targetMax = (questTarget == QuestTarget.TotalClicks) ? Mathf.Max(1, carrotTarget) : Mathf.Max(1, carrotTarget);

            SetProgress(trackedValue, targetMax);

            if (trackedValue >= carrotTarget)
            {
                // Ensure UI shows full
                SetProgress(carrotTarget, carrotTarget);

                // Mark completed and grant reward once (reward applies only to carrots)
                questCompleted = true;

                if (rewardCarrotsOnComplete > 0 && CurrencySyncManager.Instance != null)
                {
                    CurrencySyncManager.Instance.AddCurrency(rewardCarrotsOnComplete);
                    Debug.Log($"QuestProgressBar: added {rewardCarrotsOnComplete} carrots on quest completion.");
                }

                onQuestCompleted?.Invoke();
                OnQuestCompleted?.Invoke();
            }
        }
    }

    private int GetTrackedValue()
    {
        switch (questTarget)
        {
            case QuestTarget.TotalClicks:
                return AchievementManager.Instance != null ? AchievementManager.Instance.TotalClicks : 0;
            case QuestTarget.TotalCarrots:
            default:
                return CurrencySyncManager.Instance != null ? CurrencySyncManager.Instance.Carrots : 0;
        }
    }

    // Expose target change if needed
    public void SetCarrotTarget(int newTarget)
    {
        carrotTarget = Mathf.Max(1, newTarget);
    }

    public int GetCarrotTarget() => carrotTarget;

    // Coroutine updates the NextQuestIn text on minute boundaries (updates once per minute)
    private IEnumerator NextQuestTimer()
    {
        // initial update
        UpdateNextQuestText();

        while (true)
        {
            DateTime tzNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralEuropeTimeZone);
            int secondsToNextMinute = 60 - tzNow.Second;
            // wait until the start of the next minute (keeps updates aligned to minutes)
            yield return new WaitForSeconds(secondsToNextMinute);

            UpdateNextQuestText();

            // then wait full minute before next update
            yield return new WaitForSeconds(60f - 0.1f); // -0.1 to avoid rare drift; next loop re-aligns
        }
    }

    private void UpdateNextQuestText()
    {
        if (NextQuestIn == null) return;

        DateTime tzNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, centralEuropeTimeZone);

        // target is 21:00 local Central Europe time today or tomorrow
        DateTime target = new DateTime(tzNow.Year, tzNow.Month, tzNow.Day, 21, 0, 0);

        if (tzNow >= target)
            target = target.AddDays(1);

        TimeSpan remaining = target - tzNow;

        // Format: "Next quest in: 02h 15m"
        int hours = (int)remaining.TotalHours;
        int minutes = remaining.Minutes;

        // If less than 1 hour, show minutes only
        string formatted;
        if (hours > 0)
            formatted = $"Next quest in: {hours}h {minutes}m";
        else
            formatted = $"Next quest in: {minutes}m";

        NextQuestIn.text = formatted;
    }

    // Try common timezone IDs for Central European Time (handles Windows and Linux ids), fallback to fixed +1 if none found.
    private TimeZoneInfo ResolveCentralEuropeTimeZone()
    {
        string[] tzCandidates = new[]
        {
            "W. Europe Standard Time",
            "Central Europe Standard Time",
            "Romance Standard Time",
            "Europe/Paris",
            "Europe/Berlin"
        };

        foreach (var id in tzCandidates)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                if (tz != null) return tz;
            }
            catch { }
        }

        // Fallback: create fixed offset +1 (no DST).
        return TimeZoneInfo.CreateCustomTimeZone("CET-Approx", TimeSpan.FromHours(1), "CET-Approx", "CET-Approx");
    }

    // Optional: reset the quest tracking (useful for testing)
    public void ResetQuest()
    {
        questCompleted = false;
        lastTrackedValue = -1;
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (progressText != null) progressText.text = "0%";
    }
}