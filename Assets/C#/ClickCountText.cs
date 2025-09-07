using UnityEngine;
using TMPro;

public class ClickCountText : MonoBehaviour
{
public TMP_Text clickCountText;

void Update()
{
    clickCountText.text = FormatNumber(ClickManager.Instance.ClickCount) + " Clicks";
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