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
        InitializationOptions options = new InitializationOptions();
        string profileName = "P_" + System.Diagnostics.Process.GetCurrentProcess().Id;
        profileName = profileName.Length > 30 ? profileName.Substring(0, 30) : profileName;
        options.SetProfile(profileName);

        await UnityServices.InitializeAsync(options);

        if (AuthenticationService.Instance.IsSignedIn == false)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        RemoteConfigManager.Instance.FetchConfigs();
    }

    private void OnConfigsFetchCompleted()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
