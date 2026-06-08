using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyCardInfoPopupUI : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }
}
