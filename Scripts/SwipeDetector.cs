using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeDetector : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public GuideCardController controller;
    public float minSwipeDistance = 80f;

    private Vector2 dragStart;

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStart = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float deltaX = eventData.position.x - dragStart.x;
        if (Mathf.Abs(deltaX) < minSwipeDistance) return;

        if (deltaX < 0) controller.OnSwipeLeft();
        else controller.OnSwipeRight();
    }
}