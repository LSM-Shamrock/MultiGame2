using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class Core : FieldObject
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SpriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Blue");
        }
        else
        {
            SpriteRenderer.sprite = Resources.Load<Sprite>("CoreSprite/Core_Red");
        }
    }
}
