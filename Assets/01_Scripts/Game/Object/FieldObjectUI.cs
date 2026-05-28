using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public abstract class FieldObjectUI : NetworkBehaviour
{
    protected abstract FieldObject Object { get; }
    protected abstract Image HealthBarFill { get; }

    protected Camera _camera;
    protected int _maxHealth;
    protected int _currentHealth;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            HealthBarFill.color = new Color(0f, 0.6f, 1f);
        }
        else
        {
            HealthBarFill.color = new Color(1f, 0f, 0f);
        }

        _camera = Camera.main;

        _maxHealth = Object.MaxHealth.Value;
        _currentHealth = Object.CurrentHealth.Value;
        RefreshHealthBar();

        Object.MaxHealth.OnValueChanged += OnMaxHealthChanged;
        Object.CurrentHealth.OnValueChanged += OnCurrentHealthChanged;
    }

    private void OnCurrentHealthChanged(int prevValue, int newValue)
    {
        Debug.Log($"{Object.gameObject.name} {newValue}");
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

        HealthBarFill.fillAmount = (float)_currentHealth / _maxHealth;
    }
}
