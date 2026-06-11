using UnityEngine;

public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static T Instance => _instance != null ? _instance : (_instance = FindAnyObjectByType<T>());
    private static T _instance;

    protected void InitSingleton()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }
}
