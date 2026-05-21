using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;

    private void Start()
    {
        _localPlayerNameText.text = LobbyManager.Instance.PlayerName;
        _otherPlayerNameText.text = LobbyManager.Instance.OpponentPlayerSessionData.PlayerName;
    }
}
