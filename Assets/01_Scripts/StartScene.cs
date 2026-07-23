using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

[AutoInjectionTarget]
public class StartScene : MonoBehaviour
{
    [SerializeField, SceneComponentField] private RemoteConfigManager RemoteConfigManager;
    [SerializeField, SceneComponentField] private LobbyManager LobbyManager;
    [SerializeField, SceneComponentField] private MatchingManager MatchingManager;
    [SerializeField, SceneComponentField] private PopupManager PopupManager;
    [SerializeField, SceneComponentField] private SoundManager SoundManager;
    [SerializeField, SceneComponentField] private ScreenManager ScreenManager;
    [SerializeField, SceneComponentField] private SaveManager SaveManager;

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

        RemoteConfigManager.Initialize();
        RemoteConfigManager.FetchConfigs();
    }

    private void OnConfigsFetchCompleted()
    {
        LobbyManager.Initialize();
        MatchingManager.Initialize();
        PopupManager.Initialize();
        SoundManager.Initialize();
        ScreenManager.Initialize();
        SaveManager.Initialize();

        SceneManager.LoadScene("LobbyScene");
    }
}
