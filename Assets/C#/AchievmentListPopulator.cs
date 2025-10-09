using UnityEngine;

public class AchievmentListPopulator : MonoBehaviour
{
    [SerializeField] private GameObject achievementPrefab; // prefab with Achievment_list_prefab component
    [SerializeField] private Transform parentTransform;    // content parent for instantiated prefabs
    [SerializeField] private AchievementDetailPrefab detailPanel; // Reference to the detail panel

    private void Start()
    {
        if (achievementPrefab == null || parentTransform == null) return;
        if (AchievementManager.Instance == null) return;

        var achievements = AchievementManager.Instance.GetAllAchievements();

        if (achievements == null || achievements.Count == 0) return;

        // Group achievements into sets of 4
        for (int i = 0; i < achievements.Count; i += 4)
        {
            var go = Instantiate(achievementPrefab, parentTransform);
            var prefabComponent = go.GetComponent<Achievment_list_prefab>();

            if (prefabComponent != null)
            {
                // Get up to 4 achievements for this prefab instance
                AchievementItem ach1 = i < achievements.Count ? achievements[i] : null;
                AchievementItem ach2 = i + 1 < achievements.Count ? achievements[i + 1] : null;
                AchievementItem ach3 = i + 2 < achievements.Count ? achievements[i + 2] : null;
                AchievementItem ach4 = i + 3 < achievements.Count ? achievements[i + 3] : null;

                prefabComponent.SetAchievements(ach1, ach2, ach3, ach4);
            }
        }
    }
}