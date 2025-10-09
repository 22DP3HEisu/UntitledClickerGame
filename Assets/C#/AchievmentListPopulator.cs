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
        for (int i = 0; i < achievements.Count; i++)
        {
            var go = Instantiate(achievementPrefab, parentTransform);
            var dataHolder = go.GetComponent<Achievment_list_prefab>();
            if (dataHolder != null)
            {
                // populate sprite fields (you can assign different sprites if AchievementItem exposes them)
                dataHolder.image = achievements[i].icon;
                dataHolder.image2 = achievements[i].icon;
                dataHolder.image3 = achievements[i].icon;
                dataHolder.image4 = achievements[i].icon;

                // ensure UI Images inside prefab are updated
                dataHolder.ApplyAssignedSprites();
            }
        }
    }
}