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
            {
                _instance.Initialize();
                _instance._isInitialize = true;
            }

            return _instance;
        }
    }

    protected bool _isInitialize;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void Initialize()
    {

    }
}
