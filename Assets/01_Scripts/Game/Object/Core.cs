using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Core : FieldObject
{
    public override Collider2D Collider => _collider;

    [SerializeField, ComponentField] private SpriteRenderer _spriteRenderer;
    [SerializeField, ComponentField] private Collider2D _collider;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            MaxHealth.Value = 10000;
            CurrentHealth.Value = 10000;
        }

        if (IsOwner)
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Blue");
        }
        else
        {
            _spriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Red");
        }
    }
}
