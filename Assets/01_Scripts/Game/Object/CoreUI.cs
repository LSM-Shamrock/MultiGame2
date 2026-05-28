using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class CoreUI : FieldObjectUI
{
    protected override FieldObject Object => _core;
    protected override Image HealthBarFill => _healthBarFill;

    [SerializeField, ParentField] private Core _core;
    [SerializeField, ChildField] private Image _healthBarFill;
}
