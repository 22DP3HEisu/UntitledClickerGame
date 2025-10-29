using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ClickPopupAnimation : MonoBehaviour
{
    [Tooltip("Vertical speed in units per second")]
    public float moveUpSpeed = 60f;

    [Tooltip("Duration of the fade in seconds (must be > 0)")]
    public float fadeDuration = 0.3f;

    private Image image;
    private Color startColor;
    private float timer = 0f;

    // Ensure component is enabled at runtime even if it was unchecked in the inspector/prefab.
    // Awake runs whether the component is enabled or not, so we can safely enable here.
    void Awake()
    {
        enabled = true;
    }

    // Called by the spawner to initialize / reset the animation for a newly created popup.
    // This makes changes to moveUpSpeed/fadeDuration visible immediately and ensures a fresh timer/start color.
    public void Initialize(Image img)
    {
        image = img;
        timer = 0f;

        if (image != null)
            startColor = image.color;
        else
            startColor = Color.white;

        enabled = true;
    }

    void Start()
    {
        // If Initialize was not called, try to grab the Image on Start.
        if (image == null)
            image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError("ClickPopupAnimation: Image component not found on popup GameObject!");
            enabled = false;
            return;
        }

        // Ensure startColor is taken from the image when animation begins.
        startColor = image.color;
    }

    void Update()
    {
        if (image == null) return;

        // Movement uses the current moveUpSpeed value every frame, so adjusting it in the Inspector during Play immediately changes behavior.
        transform.localPosition += Vector3.up * moveUpSpeed * Time.deltaTime;

        // Fade uses the current fadeDuration value each frame; changing it during Play will affect the remaining fade.
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / Mathf.Max(0.0001f, fadeDuration));
        image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= fadeDuration)
            Destroy(gameObject);
    }

    // Editor-time validation to keep sensible values and to make immediate property adjustments predictable.
    void OnValidate()
    {
        if (fadeDuration <= 0f)
            fadeDuration = 0.01f;
        if (moveUpSpeed < 0f)
            moveUpSpeed = 0f;
    }
}