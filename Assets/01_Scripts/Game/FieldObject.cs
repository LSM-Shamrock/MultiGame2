using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
    public Vector2 ColliderCenter => Collider.bounds.center;


    public float ColliderDistanceTo(FieldObject b)
    {
        var colA = Collider;
        var colB = b.Collider;

        if (colA == null || colB == null)
            return float.PositiveInfinity;

        var result = Physics2D.Distance(colA, colB).distance;

        return result;
    }
}
