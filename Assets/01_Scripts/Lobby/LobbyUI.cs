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
        PlayerNameInput.onValueChanged.AddListener(OnValueChanged_PlayerNameInput);
        MatchmakingUI.CancleButton.onClick.AddListener(OnClick_CancleButton);

        if (GameManager.Instance)
            GameManager.Instance.State.OnValueChanged += OnChanged_GameManagerState;
    }
    private void OnDestroy()
    {
        CreateButton.onClick.RemoveAllListeners();
        JoinButton.onClick.RemoveAllListeners();
        PlayButton.onClick.RemoveAllListeners();
        PlayerNameInput.onValueChanged.RemoveAllListeners();
        MatchmakingUI.CancleButton.onClick.RemoveAllListeners();

        if (GameManager.Instance)
            GameManager.Instance.State.OnValueChanged -= OnChanged_GameManagerState;
    }

    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(JoinCodeInput.text);
    }

    private void OnValueChanged_PlayerNameInput(string value)
    {
        GameManager.Instance.PlayerName = value;
    }
    private async void OnClick_CreateButton()
    {
        MatchmakingUI.JoinCodeField.text = "코드 생성 중";
        await GameManager.Instance.CreateLobbyAsync();
        MatchmakingUI.JoinCodeField.text = GameManager.Instance.LobbyId;
    }
    private async void OnClick_JoinButton()
    {
        MatchmakingUI.JoinCodeField.text  = "";
        await GameManager.Instance.JoinLobbyAsync(JoinCodeInput.text);
    }
    private async void OnClick_AutoMatching()
    {
        MatchmakingUI.JoinCodeField.text = "";
        await GameManager.Instance.AutoMatchingAsync();
    }
    private async void OnClick_CancleButton()
    {
        await GameManager.Instance.CancelMatcingAsync();
    }

    private void OnChanged_GameManagerState(GameManagerState value)
    {
        if (value == GameManagerState.Lobby) MatchmakingUI.gameObject.SetActive(false);
        if (value == GameManagerState.FindingMatching) MatchmakingUI.gameObject.SetActive(true);
        if (value == GameManagerState.CreateingMatching) MatchmakingUI.gameObject.SetActive(true);
        if (value == GameManagerState.JoiningMatching) MatchmakingUI.gameObject.SetActive(true);
        if (value == GameManagerState.StartingGame) MatchmakingUI.JoinCodeField.text = "";

        MatchmakingUI.CancleButton.interactable = value == GameManagerState.WaitingForPalyers;
        MatchmakingUI.StateText.text = value switch
        {
            GameManagerState.FindingMatching => "매칭 찾는 중",
            GameManagerState.CreateingMatching => "매칭 생성 중",
            GameManagerState.JoiningMatching => "매칭 입장 중",
            GameManagerState.WaitingForPalyers => "다른 플레이어 입장 대기 중",
            GameManagerState.CancellingMatching => "매칭 취소 중",
            GameManagerState.StartingGame => "게임 시작 중",
            _ => "",
        };
    }
}
