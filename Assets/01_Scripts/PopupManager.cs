using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPopupUI 
{
    public Canvas Canvas { get; }
    public GameObject GameObject { get; } 
}

public class PopupManager : SingletonBehaviour<PopupManager>
{
    private Dictionary<Type, Queue<IPopupUI>> _closedPopupPools = new();
    private Stack<IPopupUI> _showingPopupStack = new();
    private int _nextSortingOrder = 10;

    public T ShowPopup<T>() where T : IPopupUI
    {
        if (_closedPopupPools.TryGetValue(typeof(T), out var pool) == false)
            _closedPopupPools.Add(typeof(T), pool = new Queue<IPopupUI>());

        T popup = default;
        if (pool.Count == 0)
        {
            var prefab = Resources.Load<GameObject>(typeof(T).Name);
            var go = Instantiate(prefab, transform);
            popup = go.GetComponent<T>();
        }
        else
        {
            popup = (T)pool.Dequeue();
        }

        _showingPopupStack.Push(popup);
        popup.Canvas.sortingOrder = _nextSortingOrder++;
        popup.GameObject.SetActive(true);

        return popup;
    }
    public void ClosePopup() 
    {
        var popup = _showingPopupStack.Pop();
        var popupType = popup.GetType();

        if (_closedPopupPools.TryGetValue(popupType, out var pool) == false)
            _closedPopupPools.Add(popupType, pool = new Queue<IPopupUI>());

        _nextSortingOrder--;

        popup.GameObject.SetActive(false);
        pool.Enqueue(popup);
    }
    public void ClosePopup(IPopupUI target)
    {
        if (target == null) return;
        if (_showingPopupStack.Peek() == target)
            ClosePopup();
    }

    private void Awake()
    {
        InitSingleton();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_showingPopupStack.Count == 0)
                ShowPopup<UI_SettingPopup>();
            else
                ClosePopup();
        }
    }
}
