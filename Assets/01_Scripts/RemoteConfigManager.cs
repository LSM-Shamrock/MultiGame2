using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Unity.Services.RemoteConfig;
using UnityEngine;

struct UserAttributes { }
struct AppAttributes { }

public class RemoteConfigManager : SingletonBehaviour<RemoteConfigManager>
{
    public event Action OnConfigsFetchCompleted;
    public IObservOnlyValue<string> GameDataVersion => _gameDataVersion;
    public IObservOnlyValue<GameData> GameData => _gameData;

    private UserAttributes _userAttributes = new();
    private AppAttributes _appAttributes = new();
    private ObservableValue<string> _gameDataVersion = new();
    private ObservableValue<GameData> _gameData = new();

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
        _gameDataVersion.Value = RemoteConfigService.Instance.appConfig.GetString("GameDataVersion");
        _gameData.Value = JsonConvert.DeserializeObject<GameData>(RemoteConfigService.Instance.appConfig.GetJson("GameData"));

        OnConfigsFetchCompleted?.Invoke();
    }
}