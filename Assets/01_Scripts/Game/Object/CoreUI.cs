using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class CoreUI : FieldObjectUI
{
    protected override FieldObject Object => _core;
    protected override Image HealthBarFillBack => _healthBarFillBack;
    protected override Image HealthBarFillFront => _healthBarFillFront;

    [SerializeField, ParentField] private Core _core;
    [SerializeField, ChildField] private Image _healthBarFillBack;
    [SerializeField, ChildField] private Image _healthBarFillFront;
    [SerializeField, ChildField] private TextMeshProUGUI _healthText;
    [SerializeField, AssetField("Sfx_Game_Unit_Hit")] private AudioClip _sfx_hit;


    protected override void LateUpdate()
    {
        base.LateUpdate();

        _healthText.text = $"{_currentHealth}/{_maxHealth}";
    }

    protected override void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        base.OnCurrentHealthChanged(prevValue, newValue);

        if (newValue < prevValue)
        {
            if (IsOwner)
                ISceneInstance<CameraEffectController>.SceneInstance.DamageEffect((prevValue - newValue) / (float)_maxHealth);

            SoundManager.Instance.PlaySfx(_sfx_hit);
        }
    }
}
