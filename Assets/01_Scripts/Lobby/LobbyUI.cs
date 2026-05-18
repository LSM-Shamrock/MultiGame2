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

        LobbyManager.Instance.IsMatchingInProgress.OnValueChanged += OnChanged_MatchingInProgress;
    }

    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(JoinCodeInput.text);
    }

    private async void OnClick_CreateButton()
    {
        MatchmakingUI.JoinCodeText.text = "방 생성 중";

        await LobbyManager.Instance.CreateRoomAndCodeAsync();

        MatchmakingUI.JoinCodeText.text = LobbyManager.Instance.JoinCode;
    }
    private async void OnClick_JoinButton()
    {
        MatchmakingUI.JoinCodeText.text = "방 접속 중";

        string inputJoinCode = JoinCodeInput.text;

        if (await LobbyManager.Instance.JoinRoomWithCodeAsync(inputJoinCode))
        {
            MatchmakingUI.JoinCodeText.text = inputJoinCode;
        }
    }
    private async void OnClick_AutoMatching()
    {
        MatchmakingUI.JoinCodeText.text = "자동 매칭";

        await LobbyManager.Instance.AutoMatchingAsync();
    }
    private void OnClick_CancleButton()
    {
        LobbyManager.Instance.CancleRoom();
    }


    private void OnChanged_MatchingInProgress(bool value)
    {
        MatchmakingUI.gameObject.SetActive(value);
    }
}
