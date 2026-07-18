using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_DeckEdit : MonoBehaviour, ISceneInstance<UI_DeckEdit>
{
    [ChildField] public GameObject RootPanel;
    [ChildField] public PointerEventBinder BackPanel;
    [ChildField] public Button ConfirmButton;

    private void Awake()
    {
        ((ISceneInstance<UI_DeckEdit>)this).InitSceneInstance();

        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
        ConfirmButton.onClick.AddListener(Hide);
    }

    public void Hide()
    {
        RootPanel.SetActive(false);
    }
    public void Show()
    {
        RootPanel.SetActive(true);
    }
}
