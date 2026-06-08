using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class SettingPopupUI : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;
    [SerializeField, ChildField] private Slider BgmVolumeSlider;
    [SerializeField, ChildField] private Slider SfxVolumeSlider;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);

        BgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeSliderChanged);
        SfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);

        BgmVolumeSlider.value = SoundManager.Instance.BgmVolume.Value;
        SfxVolumeSlider.value = SoundManager.Instance.SfxVolume.Value;
    }
    private void OnDestroy()
    {
        CloseButton.onClick.RemoveAllListeners();
        BackPanel.RemvoeEvent(PointerEventType.PointerClick, Hide);

        BgmVolumeSlider.onValueChanged.RemoveAllListeners();
        SfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    private void OnBgmVolumeSliderChanged(float value)
    {
        SoundManager.Instance.BgmVolume.Value = value;
    }
    private void OnSfxVolumeSliderChanged(float value)
    {
        SoundManager.Instance.SfxVolume.Value = value;
    }
}
