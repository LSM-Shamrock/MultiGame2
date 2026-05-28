using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UnitUI : FieldObjectUI
{
    protected override FieldObject Object => _unit;
    protected override Image HealthBarFill => _healthBarFill;

    [SerializeField, ParentField] private Unit _unit;
    [SerializeField, ChildField] private Image _healthBarFill;

    private int _unitId;
    private UnitData _unitData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _unitId = _unit.UnitId.Value;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_unitId];

        transform.localPosition = new Vector3(0f, _unitData.ColliderHeight + 0.5f);
    }
    private void LateUpdate()
    {
        transform.rotation = _camera.transform.rotation;
    }
}
