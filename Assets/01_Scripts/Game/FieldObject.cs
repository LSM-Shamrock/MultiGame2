using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
}
