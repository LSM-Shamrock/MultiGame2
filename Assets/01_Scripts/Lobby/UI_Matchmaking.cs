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

        if (GameManager.Instance)
        {
            GameManager.Instance.State.OnValueChanged += OnGameManagerStateChanged;
        }
    }
    private void OnDestroy()
    {
        CancleButton.onClick.RemoveAllListeners();
       
        if (GameManager.Instance)
        {
            GameManager.Instance.State.OnValueChanged -= OnGameManagerStateChanged;
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
        await GameManager.Instance.CancelMatcingAsync();
    }
    private void OnGameManagerStateChanged(GameManagerState value)
    {
        var creatingType = GameManager.Instance.MatchingType;

        MatchingFilterText.text = GameManager.Instance.MatchingFilter;
        LobbyIdField.text = "";
        switch (value)
        {
            case GameManagerState.Lobby: Hide(); break;
            case GameManagerState.FindingMatching: Show(); break;
            case GameManagerState.JoiningMatching: Show(); break;

            case GameManagerState.CreateingMatching:
                Debug.Log(creatingType);
                LobbyIdField.text = creatingType == MatchingType.LobbyIdMatching ? "방 아이디 생성 중" : "";
                Show();
                break;
            case GameManagerState.WaitingForPalyers:
                Debug.Log(creatingType);
                LobbyIdField.text = creatingType == MatchingType.LobbyIdMatching ? GameManager.Instance.LobbyId : "";
                break;

            case GameManagerState.StartingGame:
                SoundManager.Instance.PlaySfx(Sfx_MatchingSuccess);
                break;
        }

        CancleButton.interactable = value == GameManagerState.WaitingForPalyers;
        StateText.text = value switch
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
