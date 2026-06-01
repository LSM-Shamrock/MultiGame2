using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PointerEventType
{
    PointerClick,
    PointerDown,
    PointerUp,
    PointerEnter,
    PointerExit,
    PointerMove,
    Drag,
    BeginDrag,
    EndDrag,
}

public class PointerEventBinder : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Dictionary<PointerEventType, Action> _actions = new();
    private Dictionary<PointerEventType, Action<PointerEventData>> _dataActions = new();

    private void Call(PointerEventType type, PointerEventData eventData)
    {
        if (_actions.TryGetValue(type, out var action0)) action0?.Invoke();
        if (_dataActions.TryGetValue(type, out var action1)) action1?.Invoke(eventData);
    }

    public void AddEvent(PointerEventType type, Action action)
    {
        if (_actions.ContainsKey(type) == false)
            _actions.Add(type, action);
        else
            _actions[type] += action;
    }
    public void AddEvent(PointerEventType type, Action<PointerEventData> action)
    {
        if (_dataActions.ContainsKey(type) == false)
            _dataActions.Add(type, action);
        else
            _dataActions[type] += action;
    }

    public void RemvoeEvent(PointerEventType type, Action action)
    {
        if (_actions.ContainsKey(type))
            _actions[type] -= action;
    }
    public void RemvoeEvent(PointerEventType type, Action<PointerEventData> action)
    {
        if (_dataActions.ContainsKey(type))
            _dataActions[type] -= action;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => Call(PointerEventType.PointerClick, eventData);
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) => Call(PointerEventType.PointerDown, eventData);
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) => Call(PointerEventType.PointerUp, eventData);
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) => Call(PointerEventType.PointerEnter, eventData);
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => Call(PointerEventType.PointerExit, eventData);
    void IPointerMoveHandler.OnPointerMove(PointerEventData eventData) => Call(PointerEventType.PointerMove, eventData);
    void IDragHandler.OnDrag(PointerEventData eventData) => Call(PointerEventType.Drag, eventData);
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) => Call(PointerEventType.BeginDrag, eventData);
    void IEndDragHandler.OnEndDrag(PointerEventData eventData) => Call(PointerEventType.EndDrag, eventData);
}
