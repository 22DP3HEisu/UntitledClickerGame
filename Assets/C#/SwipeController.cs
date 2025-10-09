using UnityEngine;
using UnityEngine.EventSystems;
using System; // vajadzīgs EventAction

public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [SerializeField] int maxPage;
    int currentPage;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPageRect;

    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    private float dragThreshold;

    public event Action<int> OnPageChanged; // jauna notikuma sistēma

    private void Awake()
    {
        currentPage = 2;
        targetPos = levelPageRect.localPosition;
        dragThreshold = Screen.width / 4;
    }

    public void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > maxPage) return;

        int diff = pageNumber - currentPage;
        targetPos += pageStep * diff;
        currentPage = pageNumber;
        MovePage();
        OnPageChanged?.Invoke(currentPage); // paziņo, ka lapa mainīta
    }

    void MovePage()
    {
        levelPageRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x < eventData.pressPosition.x) GoToPage(currentPage + 1);
            else GoToPage(currentPage - 1);
        }
        else 
        {
            MovePage();
        }
    }

    public int CurrentPage => currentPage;
}