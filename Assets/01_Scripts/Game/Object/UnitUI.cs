using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UnitUI : MonoBehaviour
{
    [SerializeField, ParentField] private Unit _unit;
    [SerializeField, ChildField] private Image _healthBarFill;

    private int _maxHealth;
    private int _currentHealth;

    private void Start()
    {
        if (_unit.IsSpawned)
        {
            _maxHealth = _unit.MaxHealth.Value;
            _currentHealth = _unit.CurrentHealth.Value;
            RefreshHealthBar();
        }
        _unit.MaxHealth.OnValueChanged += OnMaxHealthChanged;
        _unit.CurrentHealth.OnValueChanged += OnCurrentHealthChanged;
    }

    private void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        _currentHealth = newValue;
        RefreshHealthBar();
    }
    private void OnMaxHealthChanged(int prevValue, int newValue)
    {
        _maxHealth = newValue;
        RefreshHealthBar();
    }

    private void RefreshHealthBar()
    {
        if (_maxHealth == 0)
            return;

        _healthBarFill.fillAmount = (float)_currentHealth / _maxHealth;
    }
}
