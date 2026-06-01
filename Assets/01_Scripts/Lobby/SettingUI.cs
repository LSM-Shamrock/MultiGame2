using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class SettingUI : MonoBehaviour
{
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;
    [SerializeField, ChildField] private Slider BgmVolumeSlider;
    [SerializeField, ChildField] private Slider SfxVolumeSlider;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }
    private void OnDestroy()
    {
        CloseButton.onClick.RemoveAllListeners();
        BackPanel.RemvoeEvent(PointerEventType.PointerClick, Hide);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
