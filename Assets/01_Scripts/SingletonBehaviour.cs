using UnityEngine;

public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null) 
                _instance = FindAnyObjectByType<T>();

            if (_instance == null)
                return null;

            if (_instance._isInitialize == false)
                _instance.Initialize();

            return _instance;
        }
    }

    protected bool _isInitialize;

    protected void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        _instance.Initialize();
    }

    protected virtual void Initialize()
    {
        _instance._isInitialize = true;
        DontDestroyOnLoad(gameObject);
    }
}
