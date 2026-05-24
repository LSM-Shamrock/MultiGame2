using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEventHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public event Action<PointerEventData> OnPointerClick;
    public event Action<PointerEventData> OnPointerDown;
    public event Action<PointerEventData> OnPointerUp;
    public event Action<PointerEventData> OnPointerEnter;
    public event Action<PointerEventData> OnPointerExit;
    public event Action<PointerEventData> OnPointerMove;
    public event Action<PointerEventData> OnDrag;
    public event Action<PointerEventData> OnBeginDrag;
    public event Action<PointerEventData> OnEndDrag;

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => OnPointerClick?.Invoke(eventData);
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) => OnPointerDown?.Invoke(eventData);
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) => OnPointerUp?.Invoke(eventData);
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => OnPointerEnter?.Invoke(eventData);
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => OnPointerExit?.Invoke(eventData);
    void IPointerMoveHandler.OnPointerMove(PointerEventData eventData) => OnPointerMove?.Invoke(eventData);
    void IDragHandler.OnDrag(PointerEventData eventData) => OnDrag?.Invoke(eventData);
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) => OnBeginDrag?.Invoke(eventData);
    void IEndDragHandler.OnEndDrag(PointerEventData eventData) => OnEndDrag?.Invoke(eventData);
}
