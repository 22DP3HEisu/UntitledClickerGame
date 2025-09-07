using UnityEngine;
using TMPro;

public class ClickPopupAnimation : MonoBehaviour
{
    public float moveUpSpeed = 60f; 
    public float fadeDuration = 0.3f; 
    private TextMeshProUGUI tmpText;
    private Color startColor;
    private float timer = 0f;

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        if (tmpText == null)
        {
            Debug.LogError("TextMeshProUGUI component not found on popup GameObject!");
            enabled = false;
            return;
        }
        startColor = tmpText.color;
    }

    void Update()
    {
        if (tmpText == null) return;

        // Move up
        transform.localPosition += Vector3.up * moveUpSpeed * Time.deltaTime;

        // Fade out
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        tmpText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= fadeDuration)
            Destroy(gameObject);
    }
}