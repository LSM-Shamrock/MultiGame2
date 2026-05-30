using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class HitEffectPool : NetworkBehaviour
{
    public static HitEffectPool Instance => _instance ?? (_instance = FindAnyObjectByType<HitEffectPool>());
    private static HitEffectPool _instance;

    private Queue<HitEffect> _hitEffects = new();

    [SerializeField, AssetField("HitEffect")] 
    private GameObject _hitEffectPrefab;

    private void Start()
    {
        for (int i = 0; i < 10; i++)
            _hitEffects.Enqueue(CreateHitEffect());
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowHitEffectRpc(int attackHitId, Vector2 postion)
    {
        var attackHitData = StaticDB.Instance.AttackHitData.Dictionary[attackHitId];

        GetOrCreateHitEffect().Show(attackHitData, postion);
    }

    private HitEffect GetOrCreateHitEffect()
    {
        if (_hitEffects.TryDequeue(out var hitEffect))
        {
            return hitEffect;
        }
        else
        {
            return CreateHitEffect();
        }
    }
    private HitEffect CreateHitEffect()
    {
        var go = Instantiate<GameObject>(_hitEffectPrefab, transform);
        var hitEffect = go.GetComponent<HitEffect>();

        go.SetActive(false);

        return hitEffect;
    }

    public void ReturnToPool(HitEffect hitEffect)
    {
        _hitEffects.Enqueue(hitEffect);
    }
}
