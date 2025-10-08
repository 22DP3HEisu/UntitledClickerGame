using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;

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

    private FieldInfo pointerStartLocalCursorField;
    private FieldInfo contentStartPositionField;

    void Awake()
    {
        // Prepare reflection to patch ScrollRect private fields
        pointerStartLocalCursorField = typeof(ScrollRect).GetField("m_PointerStartLocalCursor", BindingFlags.NonPublic | BindingFlags.Instance);
        contentStartPositionField = typeof(ScrollRect).GetField("m_ContentStartPosition", BindingFlags.NonPublic | BindingFlags.Instance);

        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForEndOfFrame();

        if (horizontalScrollView != null)
        {
            horizontalScrollView.StopMovement();
            horizontalScrollView.enabled = false;
        }
        if (verticalScrollView != null)
        {
            verticalScrollView.StopMovement();
            verticalScrollView.enabled = false;
        }

        yield return new WaitForSeconds(startDelay);
        isReady = true;
        Debug.Log("[SwipeController] Ready for input");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        dragStartPos = eventData.position;
        swipeDetected = false;
        isHorizontalSwipe = false;
        isVerticalSwipe = false;

        horizontalScrollView?.StopMovement();
        verticalScrollView?.StopMovement();

        horizontalScrollView.enabled = false;
        verticalScrollView.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        if (swipeDetected)
        {
            if (isHorizontalSwipe && horizontalScrollView.enabled)
                horizontalScrollView.OnDrag(eventData);
            else if (isVerticalSwipe && verticalScrollView.enabled)
                verticalScrollView.OnDrag(eventData);
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
                EnableScroll(horizontalScrollView, eventData);
            }
            else
            {
                isVerticalSwipe = true;
                EnableScroll(verticalScrollView, eventData);
            }
        }
    }

    private void EnableScroll(ScrollRect scroll, PointerEventData eventData)
    {
        if (scroll == null) return;

        scroll.StopMovement();
        scroll.velocity = Vector2.zero;
        scroll.enabled = true;

        // --- Fix snap: manually set starting drag data ---
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scroll.viewport ?? scroll.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localCursor);

        pointerStartLocalCursorField?.SetValue(scroll, localCursor);
        contentStartPositionField?.SetValue(scroll, scroll.content.anchoredPosition);
        // --------------------------------------------------

        scroll.OnBeginDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isReady) return;

        if (isHorizontalSwipe && horizontalScrollView.enabled)
        {
            horizontalScrollView.OnEndDrag(eventData);
            horizontalScrollView.StopMovement();
            horizontalScrollView.enabled = false;
        }
        else if (isVerticalSwipe && verticalScrollView.enabled)
        {
            verticalScrollView.OnEndDrag(eventData);
            verticalScrollView.StopMovement();
            verticalScrollView.enabled = false;
        }

        swipeDetected = false;
        isHorizontalSwipe = false;
        isVerticalSwipe = false;
    }
}
