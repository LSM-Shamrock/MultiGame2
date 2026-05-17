using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyUI : MonoBehaviour
{
    [ChildField] public Button PlayButton;
    [ChildField] public Button CreateButton;
    [ChildField] public Button JoinButton;
    [ChildField] public TMP_InputField JoinCodeInput;
    [ChildField] public TMP_InputField PlayerNameInput;
    [SceneComponentField] public MatchmakingUI MatchmakingUI;

    private void Start()
    {
        CreateButton.onClick.AddListener(OnClick_CreateButton);
        JoinButton.onClick.AddListener(OnClick_JoinButton);
        PlayButton.onClick.AddListener(OnClick_AutoMatching);
        MatchmakingUI.CancleButton.onClick.AddListener(OnClick_CancleButton);
    }

    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(JoinCodeInput.text);
    }

    private async void OnClick_CreateButton()
    {
        await LobbyManager.Instance.CreateRoomAndCodeAsync();

        MatchmakingUI.JoinCodeText.text = LobbyManager.Instance.JoinCode;
        MatchmakingUI.gameObject.SetActive(true);
    }
    private async void OnClick_JoinButton()
    {
        string inputJoinCode = JoinCodeInput.text;

        if (await LobbyManager.Instance.JoinRoomWithCodeAsync(inputJoinCode))
        {
            MatchmakingUI.JoinCodeText.text = inputJoinCode;
            MatchmakingUI.gameObject.SetActive(true);
        }
    }
    private async void OnClick_AutoMatching()
    {
        await LobbyManager.Instance.AutoMatchingAsync();

        MatchmakingUI.JoinCodeText.text = "매치메이킹";
        MatchmakingUI.gameObject.SetActive(true);
    }
    private void OnClick_CancleButton()
    {
        LobbyManager.Instance.CancleRoom();
        MatchmakingUI.gameObject.SetActive(false);
    }

}
