using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class EffectPool : NetworkBehaviour, ISceneInstance<EffectPool>
{
    private Queue<HitEffect> _hitEffects = new();
    private Queue<DotEffect> _dotEffects = new();

    [SerializeField, AssetField("HitEffect")] private GameObject _hitEffectPrefab;
    [SerializeField, AssetField("DotEffect")] private GameObject _DottEffectPrefab;


    private void Start()
    {
        ((ISceneInstance<EffectPool>)this).InitSceneInstance();

        for (int i = 0; i < 1; i++)
            _hitEffects.Enqueue(CreateHitEffect());

        for (int i = 0; i < 1; i++)
            _dotEffects.Enqueue(CreateDotEffect());
    }

    public void ReturnHitEffectToPool(HitEffect hitEffect)
    {
        _hitEffects.Enqueue(hitEffect);
    }
    public void ReturnDotEffectToPool(DotEffect dotEffect)
    {
        _dotEffects.Enqueue(dotEffect);
    }

    private HitEffect CreateHitEffect()
    {
        var go = Instantiate<GameObject>(_hitEffectPrefab, transform);
        var effect = go.GetComponent<HitEffect>();
        go.SetActive(false);
        return effect;
    }
    private DotEffect CreateDotEffect()
    {
        var go = Instantiate<GameObject>(_DottEffectPrefab, transform);
        var effect = go.GetComponent<DotEffect>();
        go.SetActive(false);
        return effect;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowHitEffectRpc(int attackHitId, Vector2 postion)
    {
        var data = RemoteConfigManager.Instance.GameData.Value.AttackHitData.Dictionary[attackHitId];

        if (_hitEffects.TryDequeue(out var hitEffect) == false)
            hitEffect = CreateHitEffect();

        hitEffect.Show(data, postion);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void ShowDotEffectRpc(ulong targetNetworkObjectId, int dotEffectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetNetworkObject))
        {
            var target = targetNetworkObject.GetComponent<FieldObject>();

            var data = RemoteConfigManager.Instance.GameData.Value.DotEffectData.Dictionary[dotEffectId];

            if (_dotEffects.TryDequeue(out var effect) == false)
                effect = CreateDotEffect();

            effect.Show(data, target);
        }
    }
}
