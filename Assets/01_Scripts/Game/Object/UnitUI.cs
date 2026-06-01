using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UnitUI : FieldObjectUI
{
    protected override FieldObject Object => _unit;
    protected override Image HealthBarFillBack => _healthBarFillBack;
    protected override Image HealthBarFillFront => _healthBarFillFront;

    [SerializeField, ParentField] private Unit _unit;
    [SerializeField, ChildField] private Image _healthBarFillBack;
    [SerializeField, ChildField] private Image _healthBarFillFront;
    [SerializeField, AssetField("Sfx_Game_Unit_Summon")] private AudioClip _sfx_summon;
    [SerializeField, AssetField("Sfx_Game_Unit_Hit")] private AudioClip _sfx_hit;

    private int _unitId;
    private UnitData _unitData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _unitId = _unit.UnitId.Value;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];

        transform.localPosition = new Vector3(0f, _unitData.ColliderHeight + 0.5f);

        SoundManager.Instance.PlaySfx(_sfx_summon);
    }

    protected override void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        base.OnCurrentHealthChanged(prevValue, newValue);

        if (newValue < prevValue)
        {
            SoundManager.Instance.PlaySfx(_sfx_hit);
        }
    }
}
