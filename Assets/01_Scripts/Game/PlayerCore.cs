using Unity.Netcode;
using UnityEngine;

public class PlayerCore : NetworkBehaviour
{
    public NetworkVariable<float> PlayerMP { get; private set; } = new(4, readPerm: NetworkVariableReadPermission.Owner);

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.PlayerCores[OwnerClientId] = this;
        Debug.Log("플레이어 코어 등록됨");
    }

    private void Update()
    {
        if (IsServer)
        {
            if (PlayerMP.Value < 10)
                PlayerMP.Value += Time.deltaTime / 2f;
        
            if (PlayerMP.Value > 10)
                PlayerMP.Value = 10;
        }
    }
}
