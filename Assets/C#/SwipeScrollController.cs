using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SwipeScrollController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ScrollRect horizontalScrollView;
    [SerializeField] private ScrollRect verticalScrollView;
    [SerializeField] private float swipeThreshold = 10f;
    [SerializeField] private float startDelay = 1f;

    private Vector2 dragStartPos;
    private bool isHorizontalSwipe;
    private bool isVerticalSwipe;
    private bool swipeDetected;
    private bool isReady = false;

    void Awake()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForEndOfFrame();

        SafeStopAndDisable(horizontalScrollView, "horizontal (init)");
        SafeStopAndDisable(verticalScrollView, "vertical (init)");

        yield return new WaitForSeconds(startDelay);
        isReady = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        dragStartPos = eventData.position;
        swipeDetected = false;
        isHorizontalSwipe = false;
        isVerticalSwipe = false;

        SafeStopAndDisable(horizontalScrollView, "horizontal (begin)");
        SafeStopAndDisable(verticalScrollView, "vertical (begin)");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        if (swipeDetected)
        {
            if (isHorizontalSwipe && horizontalScrollView != null && horizontalScrollView.enabled)
            {
                horizontalScrollView.OnDrag(eventData);
            }
            else if (isVerticalSwipe && verticalScrollView != null && verticalScrollView.enabled)
            {
                verticalScrollView.OnDrag(eventData);
            }
            return;
        }

        Vector2 dragDelta = eventData.position - dragStartPos;
        float horizontalDistance = Mathf.Abs(dragDelta.x);
        float verticalDistance = Mathf.Abs(dragDelta.y);

        if (horizontalDistance > swipeThreshold || verticalDistance > swipeThreshold)
        {
            swipeDetected = true;

            if (horizontalDistance > verticalDistance)
            {
                isHorizontalSwipe = true;
                if (horizontalScrollView != null)
                {
                    horizontalScrollView.StopMovement();
                    horizontalScrollView.velocity = Vector2.zero;
                    horizontalScrollView.enabled = true;
                    horizontalScrollView.OnBeginDrag(eventData);
                }
            }
            else
            {
                isVerticalSwipe = true;
                if (verticalScrollView != null)
                {
                    verticalScrollView.StopMovement();
                    verticalScrollView.velocity = Vector2.zero;
                    verticalScrollView.enabled = true;
                    verticalScrollView.OnBeginDrag(eventData);
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        if (isHorizontalSwipe && horizontalScrollView != null && horizontalScrollView.enabled)
        {
            horizontalScrollView.OnEndDrag(eventData);
            horizontalScrollView.StopMovement();
            horizontalScrollView.enabled = false;
        }
        else if (isVerticalSwipe && verticalScrollView != null && verticalScrollView.enabled)
        {
            verticalScrollView.OnEndDrag(eventData);
            verticalScrollView.StopMovement();
            verticalScrollView.enabled = false;
        }

        swipeDetected = false;
        isHorizontalSwipe = false;
        isVerticalSwipe = false;
    }

    private void SafeStopAndDisable(ScrollRect sr, string tag)
    {
        if (sr == null) return;

        sr.StopMovement();
        sr.velocity = Vector2.zero;
        sr.enabled = false;
    }
}