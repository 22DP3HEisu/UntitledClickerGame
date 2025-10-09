using UnityEngine;
using UnityEngine.UI;

public class Achievment_list_prefab : MonoBehaviour
{
    // Sprites assigned by populator or in the Inspector
    [SerializeField] public Sprite image;
    [SerializeField] public Sprite image2;
    [SerializeField] public Sprite image3;
    [SerializeField] public Sprite image4;

    // UI Image components inside the prefab to display the sprites
    [SerializeField] private Image clickerImage;
    [SerializeField] private Image clickerImage2;
    [SerializeField] private Image clickerImage3;
    [SerializeField] private Image clickerImage4;

    // Apply the assigned Sprite fields to the Image components
    public void ApplyAssignedSprites()
    {
        if (clickerImage != null && image != null) clickerImage.sprite = image;
        if (clickerImage2 != null && image2 != null) clickerImage2.sprite = image2;
        if (clickerImage3 != null && image3 != null) clickerImage3.sprite = image3;
        if (clickerImage4 != null && image4 != null) clickerImage4.sprite = image4;
    }

    // Convenience: allow setting all images at once
    public void SetAllSprites(Sprite s1, Sprite s2, Sprite s3, Sprite s4)
    {
        image = s1;
        image2 = s2;
        image3 = s3;
        image4 = s4;
        ApplyAssignedSprites();
    }

#if UNITY_EDITOR
    // For inspector preview when you change sprite fields in editor
    private void OnValidate()
    {
        ApplyAssignedSprites();
    }
#endif
}