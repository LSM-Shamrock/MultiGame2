using UnityEngine;

public class CameraController : MonoBehaviour, ISceneInstance<CameraController>
{
    private void Start()
    {
        ((ISceneInstance<CameraController>)this).InitSceneInstance();
    }
}
