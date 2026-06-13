using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_SettingPopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;
    [SerializeField, ChildField] private Slider BgmSlider;
    [SerializeField, ChildField] private Slider SfxSlider;
    [SerializeField, ChildField] private TextMeshProUGUI BgmText;
    [SerializeField, ChildField] private TextMeshProUGUI SfxText;
    [SerializeField, ChildField] private TextMeshProUGUI GameDataVersionText;
    [SerializeField, ChildField] private TextMeshProUGUI ProfileText;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);

        BgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        SfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        BgmSlider.value = SoundManager.Instance.BgmVolume.Value;
        SfxSlider.value = SoundManager.Instance.SfxVolume.Value;

        RemoteConfigManager.Instance.GameDataVersion.AddListenerAndCall(OnGameDataVersionChanged);

        ProfileText.text = AuthenticationService.Instance.Profile;
    }
    private void OnDestroy()
    {
        CloseButton.onClick.RemoveAllListeners();
        BackPanel.RemvoeEvent(PointerEventType.PointerClick, Hide);

        BgmSlider.onValueChanged.RemoveAllListeners();
        SfxSlider.onValueChanged.RemoveAllListeners();
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    private void OnBgmSliderChanged(float value)
    {
        int pct = Mathf.RoundToInt(value * 20f) * 5;
        float v = pct / 100f;

        BgmText.text = $"{pct}%";
        BgmSlider.value = v;
        SoundManager.Instance.BgmVolume.Value = v;
    }
    private void OnSfxSliderChanged(float value)
    {
        int pct = Mathf.RoundToInt(value * 20f) * 5;
        float v = pct / 100f;

        SfxText.text = $"{pct}%";
        SfxSlider.value = v;
        SoundManager.Instance.SfxVolume.Value = v;
    }
    private void OnGameDataVersionChanged(string value)
    {
        GameDataVersionText.text = $"게임 데이터 버전: v{value}";
    }
}
