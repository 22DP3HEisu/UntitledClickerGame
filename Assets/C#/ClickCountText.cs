using UnityEngine;
using TMPro;

public class CarrotCountText : MonoBehaviour
{
public TMP_Text carrotCountText;

void Update()
{
    int carrots = CurrencySyncManager.Instance != null ? CurrencySyncManager.Instance.Carrots : 0;
    carrotCountText.text = FormatNumber(carrots);
}

string FormatNumber(int num)
{
    if (num >= 1_000_000_000)
        return (num / 1_000_000_000f).ToString("0.#") + "B";
    if (num >= 1_000_000)
        return (num / 1_000_000f).ToString("0.#") + "M";
    if (num >= 1_000)
        return (num / 1_000f).ToString("0.#") + "k";
    return num.ToString();
}
}