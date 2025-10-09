using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SwipeController swipeController;
    [SerializeField] private int pageNumber;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    private Vector3 startScale;
    private Image image;

    private void Awake()
    {
        startScale = transform.localScale;
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (swipeController != null)
            swipeController.OnPageChanged += OnPageChanged;
    }

    private void OnDisable()
    {
        if (swipeController != null)
            swipeController.OnPageChanged -= OnPageChanged;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        
        
        float duration = 0.1f;

        
        LeanTween.scale(gameObject, startScale * 0.9f, duration)
        .setEaseOutQuad()
        .setOnComplete(() =>
        {
            LeanTween.scale(gameObject, startScale, duration).setEaseInQuad();
        });
        if (swipeController != null)
        {
            swipeController.GoToPage(pageNumber);
            Debug.Log($"{gameObject.name} clicked → going to page {pageNumber}");
        }
    }

    private void OnPageChanged(int currentPage)
    {
        SetActiveState(currentPage == pageNumber);
    }

    private void SetActiveState(bool isActive)
    {
        if (image == null) return;

        if (activeSprite != null && inactiveSprite != null)
        {
            image.sprite = isActive ? activeSprite : inactiveSprite;
        }
        else
        {
            // ja nav sprite, mainām krāsu kā alternatīvu
            image.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
    }
}