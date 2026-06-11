using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScene : MonoBehaviour
{
    private void Awake()
    {
        RemoteConfigManager.Instance.OnConfigsFetchCompleted += OnConfigsFetchCompleted;
    }
    private void OnDestroy()
    {
        RemoteConfigManager.Instance.OnConfigsFetchCompleted -= OnConfigsFetchCompleted;
    }
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn == false)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        RemoteConfigManager.Instance.FetchConfigs();
    }

    private void OnConfigsFetchCompleted()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
