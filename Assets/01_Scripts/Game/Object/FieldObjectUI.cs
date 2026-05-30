using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public abstract class FieldObjectUI : NetworkBehaviour
{
    protected abstract FieldObject Object { get; }
    protected abstract Image HealthBarFillBack { get; }
    protected abstract Image HealthBarFillFront { get; }

    protected Camera _camera;
    protected int _maxHealth;
    protected int _currentHealth;
    protected float _displayHealth;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            HealthBarFillFront.color = new Color(0f, 0.6f, 1f);
        else
            HealthBarFillFront.color = new Color(1f, 0f, 0f);

        _camera = Camera.main;

        _maxHealth = Object.MaxHealth.Value;
        _currentHealth = Object.CurrentHealth.Value;
        _displayHealth = Object.CurrentHealth.Value;
        RefreshHealthBar();

        Object.MaxHealth.OnValueChanged += OnMaxHealthChanged;
        Object.CurrentHealth.OnValueChanged += OnCurrentHealthChanged;
    }

    protected virtual void LateUpdate()
    {
        transform.rotation = _camera.transform.rotation;

        if (_currentHealth > _displayHealth)
            HealthBarFillBack.color = Color.green;
        else
            HealthBarFillBack.color = Color.white;

        if (Mathf.Abs(_currentHealth - _displayHealth) < 1)
            _displayHealth = _currentHealth;
        else
            _displayHealth = Mathf.Lerp(_displayHealth, _currentHealth, Time.deltaTime * 1.5f);

        RefreshHealthBar();
    }

    private void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        _currentHealth = newValue;
        RefreshHealthBar();
    }
    private void OnMaxHealthChanged(int prevValue, int newValue)
    {
        _displayHealth = newValue;
        _displayHealth = newValue;
        RefreshHealthBar();
    }
    private void RefreshHealthBar()
    {
        if (_maxHealth == 0)
            return;

        float back = Mathf.Max(_currentHealth, _displayHealth);
        float front = Mathf.Min(_currentHealth, _displayHealth);
        HealthBarFillBack.fillAmount = back / _maxHealth;
        HealthBarFillFront.fillAmount = front / _maxHealth;
    }
}
