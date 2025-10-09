using UnityEngine;

public class AchievmentListPopulator : MonoBehaviour
{
    [SerializeField] private GameObject achievementPrefab; // prefab with Achievment_list_prefab component
    [SerializeField] private Transform parentTransform;    // content parent for instantiated prefabs

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
                Sprite sprite1 = i < achievements.Count ? achievements[i].icon : null;
                Sprite sprite2 = i + 1 < achievements.Count ? achievements[i + 1].icon : null;
                Sprite sprite3 = i + 2 < achievements.Count ? achievements[i + 2].icon : null;
                Sprite sprite4 = i + 3 < achievements.Count ? achievements[i + 3].icon : null;

                prefabComponent.SetAchievementSprites(sprite1, sprite2, sprite3, sprite4);
            }
        }
    }
}