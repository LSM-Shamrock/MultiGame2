using Unity.Netcode;
using UnityEngine;

public abstract class FieldObject : NetworkBehaviour
{
    public abstract Collider2D Collider { get; }
    public Vector2 ColliderCenter => Collider.bounds.center;

    public NetworkVariable<int> MaxHealth { get; } = new();
    public NetworkVariable<int> CurrentHealth { get; } = new();

    public float GetDistance(FieldObject target)
    {
        var colA = Collider;
        var colB = target.Collider;

        if (colA == null || colB == null)
            return float.PositiveInfinity;

        float result = Physics2D.Distance(colA, colB).distance;

        return result;
    }
    public float GetHorizontalDistance(FieldObject target)
    {
        var colA = Collider;
        var colB = target.Collider;

        if (colA == null || colB == null)
            return float.PositiveInfinity;

        var a = colA.bounds;
        var b = colB.bounds;

        float distanceX = Mathf.Abs(a.center.x - b.center.x) - (a.extents.x + b.extents.x);

        distanceX = Mathf.Max(distanceX, 0f);

        return distanceX;
    }

    public void TakeHit(int damage)
    {
        CurrentHealth.Value -= damage;
    }
}
