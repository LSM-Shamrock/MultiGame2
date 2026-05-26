using UnityEngine;

public class UnitShadow : MonoBehaviour
{
    private const float GROUND_Y = -2.5f;

    private void LateUpdate()
    {
        transform.position = new Vector3(transform.position.x, GROUND_Y);
    }
}
