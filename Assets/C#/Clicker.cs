using UnityEngine;
using UnityEngine.InputSystem;

public class Clicker : MonoBehaviour
{
    public Transform targetTransform;
    public Transform HorseSprite;
    public ClickPopupSpawner popupSpawner;

    private Vector3 originalScale;
    public float scaleUpFactor = 1.2f;
    public float scaleDuration = 0.1f;

    void Start()
    {
        originalScale = HorseSprite.localScale;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.transform == targetTransform)
            {
                // Increase sprite size
                HorseSprite.localScale = originalScale * scaleUpFactor;
                StartCoroutine(ResetScale());

                ClickManager.Instance.AddClicks(1);
                popupSpawner.SpawnPopup(mousePos, "+1");
            }
        }
    }
    private System.Collections.IEnumerator ResetScale()
    {
        yield return new WaitForSeconds(scaleDuration);
        HorseSprite.localScale = originalScale;
    }
}