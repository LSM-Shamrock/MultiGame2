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
}
