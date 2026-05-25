using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    [ComponentField] public BoxCollider2D Collider;
    [ComponentField] public SpriteRenderer SpriteRenderer;
}
