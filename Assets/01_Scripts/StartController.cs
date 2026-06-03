using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartController : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn == false)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        SceneManager.LoadScene("LobbyScene");
    }
}
