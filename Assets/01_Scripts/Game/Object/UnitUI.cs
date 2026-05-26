using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UnitUI : MonoBehaviour
{
    [SerializeField, ParentField] private Unit _unit;
    [SerializeField, ChildField] private Image _healthBarFill;
    
}
