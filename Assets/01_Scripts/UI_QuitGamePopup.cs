using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_QuitGamePopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button QuitButton;
    [SerializeField, ChildField] private Button CancleButton;

    private void Start()
    {
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
        CancleButton.onClick.AddListener(Hide);
        QuitButton.onClick.AddListener(OnClickQuitButton);
    }
    private void OnDestroy()
    {
        BackPanel.RemvoeEvent(PointerEventType.PointerClick, Hide);
        CancleButton.onClick.RemoveAllListeners();
        QuitButton.onClick.RemoveAllListeners();
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    private void OnClickQuitButton()
    {
        Application.Quit();
    }
}
