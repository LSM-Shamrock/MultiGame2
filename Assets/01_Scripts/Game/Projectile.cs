using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    private FieldObject _target;
    private ProjectileData _projectileData;

    public void Init(FieldObject target, ProjectileData data)
    {
        _target = target;
        _projectileData = data;
    }

    private void LateUpdate()
    {
        if (IsServer)
            transform.position += (_target.ColliderCenter - transform.position).normalized * Time.deltaTime * _projectileData.Speed;
    }
}
