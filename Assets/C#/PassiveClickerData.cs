using UnityEngine;

[System.Serializable]
public class PassiveClickerData
{
    public string name;
    public string description;
    public Sprite image;
    public int startPrice;
    public int level;
    public float clicksPerSecond;

    public PassiveClickerData(string name, string description, Sprite image, int startPrice, int level, float clicksPerSecond)
    {
        this.name = name;
        this.description = description;
        this.image = image;
        this.startPrice = startPrice;
        this.level = level;
        this.clicksPerSecond = clicksPerSecond;
    }

    public int GetCurrentPrice()
    {
        return Mathf.RoundToInt(startPrice * Mathf.Pow(1.15f, level));
    }
}