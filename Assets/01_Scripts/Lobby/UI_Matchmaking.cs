using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_Matchmaking : MonoBehaviour
{
    [ChildField] public GameObject MainPanel;
    [ChildField] public Button CancleButton;
    [ChildField] public TMP_InputField LobbyIdField;
    [ChildField] public TextMeshProUGUI StateText;
    [ChildField] public TextMeshProUGUI MatchingFilterText;
    [AssetField("Sfx_Lobby_MatchingSuccess")] public AudioClip Sfx_MatchingSuccess;

    private void Start()
    {
        CancleButton.onClick.AddListener(OnClick_MatchmaingUI_CancleButton);

        if (MatchingManager.Instance)
        {
            MatchingManager.Instance.State.OnValueChanged += OnGameManagerStateChanged;
        }
    }
    private void OnDestroy()
    {
        CancleButton.onClick.RemoveAllListeners();
       
        if (MatchingManager.Instance)
        {
            MatchingManager.Instance.State.OnValueChanged -= OnGameManagerStateChanged;
        }
    }

    private void Hide()
    {
        MainPanel.SetActive(false);
    }
    private void Show()
    {
        MainPanel.SetActive(true);
    }

    private async void OnClick_MatchmaingUI_CancleButton()
    {
        await MatchingManager.Instance.CancelMatchingAsync();
    }
    private void OnGameManagerStateChanged(MatchingManagerState value)
    {
        var creatingType = MatchingManager.Instance.MatchingType;

        MatchingFilterText.text = MatchingManager.Instance.MatchingFilter;
        LobbyIdField.text = "";
        switch (value)
        {
            case MatchingManagerState.Lobby: Hide(); break;
            case MatchingManagerState.FindingMatching: Show(); break;
            case MatchingManagerState.JoiningMatching: Show(); break;

            case MatchingManagerState.CreatingMatching:
                Debug.Log(creatingType);
                LobbyIdField.text = creatingType == MatchingType.LobbyIdMatching ? "방 아이디 생성 중" : "";
                Show();
                break;
            case MatchingManagerState.WaitingForPlayers:
                Debug.Log(creatingType);
                LobbyIdField.text = creatingType == MatchingType.LobbyIdMatching ? MatchingManager.Instance.LobbyId : "";
                break;

            case MatchingManagerState.StartingGame:
                SoundManager.Instance.PlaySfx(Sfx_MatchingSuccess);
                break;
        }

        CancleButton.interactable = value == MatchingManagerState.WaitingForPlayers;
        StateText.text = value switch
        {
            MatchingManagerState.FindingMatching => "매칭 찾는 중",
            MatchingManagerState.CreatingMatching => "매칭 생성 중",
            MatchingManagerState.JoiningMatching => "매칭 입장 중",
            MatchingManagerState.WaitingForPlayers => "다른 플레이어 입장 대기 중",
            MatchingManagerState.CancellingMatching => "매칭 취소 중",
            MatchingManagerState.StartingGame => "게임 시작 중",
            _ => "",
        };
    }
}
