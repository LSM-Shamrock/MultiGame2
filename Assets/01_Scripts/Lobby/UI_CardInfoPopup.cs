using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_CardInfoPopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;
    [SerializeField, ChildField] private TextMeshProUGUI NameText;
    [SerializeField, ChildField] private TextMeshProUGUI DescriptionText;


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
