using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyUI : MonoBehaviour
{
    [ChildField] public Button PlayButton;
    [ChildField] public Button CreateButton;
    [ChildField] public Button JoinButton;
    [ChildField] public Button SettingButton;
    [ChildField] public TMP_InputField JoinCodeInput;
    [ChildField] public TMP_InputField PlayerNameInput;
    [SceneComponentField] public MatchmakingUI MatchmakingUI;
    [SceneComponentField] public SettingUI SettingUI;
    [AssetField("Bgm_Lobby")] public AudioClip Bgm;
    [AssetField("Sfx_Lobby_MatchingSuccess")] public AudioClip Sfx_MatchingSuccess;

    private void Start()
    {
        PlayerNameInput.onValueChanged.AddListener(OnPlayerNameInputChanged);
        CreateButton.onClick.AddListener(OnClick_CreateButton);
        JoinButton.onClick.AddListener(OnClick_JoinButton);
        PlayButton.onClick.AddListener(OnClick_AutoMatching);
        MatchmakingUI.CancleButton.onClick.AddListener(OnClick_MatchmaingUI_CancleButton);

        SettingButton.onClick.AddListener(SettingUI.Show);

        if (GameManager.Instance)
        {
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged += OnDeckCardIdsChanged;
            GameManager.Instance.State.OnValueChanged += OnGameManagerStateChanged;
        }

        SoundManager.Instance.PlayBgm(Bgm);
    }
    private void OnDestroy()
    {
        CreateButton.onClick.RemoveAllListeners();
        JoinButton.onClick.RemoveAllListeners();
        PlayButton.onClick.RemoveAllListeners();
        PlayerNameInput.onValueChanged.RemoveAllListeners();
        MatchmakingUI.CancleButton.onClick.RemoveAllListeners();

        SettingButton.onClick.RemoveAllListeners();

        if (GameManager.Instance)
        {
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged -= OnDeckCardIdsChanged;
            GameManager.Instance.State.OnValueChanged -= OnGameManagerStateChanged;
        }
    }
    private void Update()
    {
        JoinButton.interactable = !string.IsNullOrEmpty(JoinCodeInput.text);
    }

    private void OnPlayerNameInputChanged(string value)
    {
        GameManager.Instance.PlayerName = value;
    }
    private void OnDeckCardIdsChanged(IReadOnlyList<int> deckCardIds)
    {
        bool valid = true;
        foreach (int cardId in deckCardIds)
        {
            if (cardId == -1)
            {
                valid = false;
                break;
            }
        }
        PlayButton.interactable = valid;
        CreateButton.interactable = valid;
        JoinButton.interactable = valid;
    }
    private void OnGameManagerStateChanged(GameManagerState value)
    {
        switch (value)
        {
            case GameManagerState.Lobby:  MatchmakingUI.gameObject.SetActive(false); break;
            case GameManagerState.FindingMatching: MatchmakingUI.gameObject.SetActive(true); break;
            case GameManagerState.CreateingMatching: MatchmakingUI.gameObject.SetActive(true); break;
            case GameManagerState.JoiningMatching: MatchmakingUI.gameObject.SetActive(true); break;

            case GameManagerState.StartingGame: 
                MatchmakingUI.JoinCodeField.text = "";
                SoundManager.Instance.PlaySfx(Sfx_MatchingSuccess);
                break;
        }

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
    private async void OnClick_MatchmaingUI_CancleButton()
    {
        await GameManager.Instance.CancelMatcingAsync();
    }
}

