using UnityEngine;


public class RemoteConfigManager : SingletonBehaviour<RemoteConfigManager>
{
    private void Awake()
    {
        InitSingleton();
    }
}