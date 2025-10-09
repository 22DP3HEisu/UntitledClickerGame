using UnityEngine;

[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievements/Achievement")]
public class AchievementData : ScriptableObject
{
    public string id;
    public new string name;
    public string description;
    public Sprite icon;

    public AchievementType type;
    public int targetValue;
    public string targetBuildingName;

    public RewardType rewardType;
    public BoostTarget boostTarget;
    public float rewardValue;
    public float temporaryDuration;
}