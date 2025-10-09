using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementDetailPrefab : MonoBehaviour
{
    public static AchievementDetailPrefab Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Image achievementIcon;
    [SerializeField] private TextMeshProUGUI achievementName;
    [SerializeField] private TextMeshProUGUI achievementDescription;
    [SerializeField] private TextMeshProUGUI achievementReward;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }

        // Hide panel initially
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowAchievementDetails(AchievementItem achievement)
    {
        if (achievement == null) return;

        // Set icon
        if (achievementIcon != null && achievement.icon != null)
        {
            achievementIcon.sprite = achievement.icon;
            achievementIcon.gameObject.SetActive(true);
        }
        else if (achievementIcon != null)
        {
            achievementIcon.gameObject.SetActive(false);
        }

        // Set name
        if (achievementName != null)
            achievementName.text = achievement.achievementName ?? "";

        // Set description
        if (achievementDescription != null)
            achievementDescription.text = achievement.description ?? "";

        // Set reward
        if (achievementReward != null)
            achievementReward.text = GetRewardText(achievement);

        // Set status text
        if (statusText != null)
        {
            if (achievement.IsCompleted)
            {
                statusText.text = "COMPLETED";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "IN PROGRESS";
                statusText.color = Color.yellow;
            }
        }

        // Show panel and bring to front
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    private string GetRewardText(AchievementItem achievement)
    {
        string reward = "Reward: ";

        switch (achievement.rewardType)
        {
            case RewardType.BoostAllPercent:
                reward += $"+{achievement.rewardValue}% Global Production";
                break;

            case RewardType.BoostAllFlat:
                reward += $"+{achievement.rewardValue} Global Production";
                break;

            case RewardType.BoostSinglePercent:
                reward += $"+{achievement.rewardValue}% {achievement.boostTarget}";
                break;

            case RewardType.BoostSingleFlat:
                reward += $"+{achievement.rewardValue} {achievement.boostTarget}";
                break;

            case RewardType.TemporaryBoost:
                reward += $"+{achievement.rewardValue}% ({achievement.temporaryDuration}s)";
                break;

            case RewardType.Unlock:
                reward += "Special Unlock";
                break;

            default:
                reward += "Unknown Reward";
                break;
        }

        return reward;
    }
}