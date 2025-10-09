using UnityEngine;
using UnityEngine.UI;

public class Achievment_list_prefab : MonoBehaviour
{
    // UI Image components inside the prefab to display the sprites
    [SerializeField] private Image achievementImage1;
    [SerializeField] private Image achievementImage2;
    [SerializeField] private Image achievementImage3;
    [SerializeField] private Image achievementImage4;

    // Set individual achievement sprites (pass null to hide that slot)
    public void SetAchievementSprites(Sprite sprite1, Sprite sprite2, Sprite sprite3, Sprite sprite4)
    {
        SetImageSprite(achievementImage1, sprite1);
        SetImageSprite(achievementImage2, sprite2);
        SetImageSprite(achievementImage3, sprite3);
        SetImageSprite(achievementImage4, sprite4);
    }

    private void SetImageSprite(Image imageComponent, Sprite sprite)
    {
        if (imageComponent != null)
        {
            if (sprite != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.enabled = true;
                imageComponent.gameObject.SetActive(true);
            }
            else
            {
                // Hide the image if no sprite is provided
                imageComponent.gameObject.SetActive(false);
            }
        }
    }
}