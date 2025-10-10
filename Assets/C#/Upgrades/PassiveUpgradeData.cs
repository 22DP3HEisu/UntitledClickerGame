using UnityEngine;

[System.Serializable]
public class PassiveUpgradeData
{
    public string id;
    public string upgradeName;
    [TextArea(1, 2)] public string description;
    public Sprite icon;
    public string targetBuildingName; // name of PassiveClickerData.name this upgrade applies to
    public int price;

    public enum RewardType
    {
        FlatClicksPerSecond, // +X clicks/sec (flat)
        PercentBoost         // +X percent multiplier to building (e.g. 10 => +10%)
    }

    public RewardType rewardType;
    public float rewardValue; // flat clicks/sec or percent value
    [HideInInspector] public bool isPurchased = false;
}