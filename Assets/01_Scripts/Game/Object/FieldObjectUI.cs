using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class FieldObjectUI : NetworkBehaviour
{
    [SerializeField, ParentField] private FieldObject _object;
    [SerializeField, ChildField] private Image _healthBarFill;

    private Camera _camera;
    private int _maxHealth;
    private int _currentHealth;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _healthBarFill.color = new Color(0f, 0.6f, 1f);
        }
        else
        {
            _healthBarFill.color = new Color(1f, 0f, 0f);
        }

        _camera = Camera.main;
        
        _maxHealth = _object.MaxHealth.Value;
        _currentHealth = _object.CurrentHealth.Value;
        RefreshHealthBar();

        _object.MaxHealth.OnValueChanged += OnMaxHealthChanged;
        _object.CurrentHealth.OnValueChanged += OnCurrentHealthChanged;
    }
    private void LateUpdate()
    {
        transform.rotation = _camera.transform.rotation;
    }

    private void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        Debug.Log($"{_object.gameObject.name} {newValue}");
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
