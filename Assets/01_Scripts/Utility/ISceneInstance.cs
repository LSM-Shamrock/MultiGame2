using UnityEngine;

public interface ISceneInstance<T> where T : MonoBehaviour, ISceneInstance<T>
{
    public static T SceneInstance => _sceneInstance != null ? _sceneInstance : (_sceneInstance = GameObject.FindAnyObjectByType<T>());
    private static T _sceneInstance;

    public void InitSceneInstance()
    {
        _sceneInstance = (T)this;
    }
}
