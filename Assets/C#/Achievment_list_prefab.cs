using UnityEngine;
using UnityEngine.UI;

public class Achievment_list_prefab : MonoBehaviour
{
    [Header("UI Image slots (one per achievement)")]
    [SerializeField] private Image achievementImage1;
    [SerializeField] private Image achievementImage2;
    [SerializeField] private Image achievementImage3;
    [SerializeField] private Image achievementImage4;

    [Header("Clickable Buttons placed over each image")]
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    [SerializeField] private Button button4;

    // Local storage of the AchievementItem for each slot
    private AchievementItem[] achievements = new AchievementItem[4];

    private void Awake()
    {
        // Ensure buttons start without stale listeners
        if (button1 != null) button1.onClick.RemoveAllListeners();
        if (button2 != null) button2.onClick.RemoveAllListeners();
        if (button3 != null) button3.onClick.RemoveAllListeners();
        if (button4 != null) button4.onClick.RemoveAllListeners();
    }

    // Populator will call this to set up the 4 slots (pass null for empty slots)
    public void SetAchievements(AchievementItem ach1, AchievementItem ach2, AchievementItem ach3, AchievementItem ach4)
    {
        achievements[0] = ach1;
        achievements[1] = ach2;
        achievements[2] = ach3;
        achievements[3] = ach4;

        ConfigureSlot(achievementImage1, button1, ach1);
        ConfigureSlot(achievementImage2, button2, ach2);
        ConfigureSlot(achievementImage3, button3, ach3);
        ConfigureSlot(achievementImage4, button4, ach4);
    }

    private void ConfigureSlot(Image img, Button btn, AchievementItem data)
    {
        if (img != null)
        {
            if (data != null && data.icon != null)
            {
                img.sprite = data.icon;
                img.gameObject.SetActive(true);
            }
            else
            {
                img.gameObject.SetActive(false);
            }
        }

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();

            if (data != null)
            {
                // capture local reference to avoid closure issue
                AchievementItem captured = data;
                btn.onClick.AddListener(() =>
                {
                    if (AchievementDetailPrefab.Instance != null)
                        AchievementDetailPrefab.Instance.ShowAchievementDetails(captured);
                    else
                        Debug.LogWarning("AchievementDetailPrefab.Instance is null - ensure the details panel exists in the scene and has AchievementDetailPrefab attached.");
                });

                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }
}