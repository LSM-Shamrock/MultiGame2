using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;

[AutoInjectionTarget]
public class UI_StatAdditionalInfoPopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private RectTransform MainPosition;
    [SerializeField, ChildField] private RectTransform Box;
    [SerializeField, ChildField] private TextMeshProUGUI TitleText;
    [SerializeField, ChildrenGroupField] private UI_StatItem[] Stats;

    private void Start()
    {
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    public void SetDisplay(Vector2 position)
    {
        MainPosition.position = position;
        Box.position = new Vector2(Box.position.x, transform.position.y);
    }
}
