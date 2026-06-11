using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Services.RemoteConfig;
using UnityEngine;

struct UserAttributes { }
struct AppAttributes { }

public class RemoteConfigManager : SingletonBehaviour<RemoteConfigManager>
{
    public string GameDataVersion { get; private set; }
    public GameData GameData { get; private set; }

    private UserAttributes _userAttributes = new();
    private AppAttributes _appAttributes = new();

    public event Action OnConfigsFetchCompleted;

    private void Awake()
    {
        InitSingleton();
    }

    public void FetchConfigs()
    {
        RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;
        RemoteConfigService.Instance.FetchCompleted += OnFetchCompleted;
        RemoteConfigService.Instance.FetchConfigs(_userAttributes, _appAttributes);
    }

    public void OnFetchCompleted(ConfigResponse response)
    {
        GameDataVersion = RemoteConfigService.Instance.appConfig.GetString("GameDataVersion");
        GameData = JsonConvert.DeserializeObject<GameData>(RemoteConfigService.Instance.appConfig.GetJson("GameData"));

        OnConfigsFetchCompleted?.Invoke();
    }
}