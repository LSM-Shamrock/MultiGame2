using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;

    private void Awake()
    {

    }

    public void OnLocalPlayerCore()
    {
        _localPlayerNameText.text = LobbyManager.Instance.PlayerName;
        _otherPlayerNameText.text = LobbyManager.Instance.OpponentPlayerSessionData.PlayerName;
        _mpBar.Initialize();
    }
}
