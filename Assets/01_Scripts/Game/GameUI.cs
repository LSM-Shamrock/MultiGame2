using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;

    public void Initialize()
    {
        _localPlayerNameText.text = LobbyManager.Instance.PlayerName;
        _otherPlayerNameText.text = LobbyManager.Instance.OpponentPlayerSessionData.PlayerName;
        _mpBar.Initialize();
        Debug.Log("UI 초기화됨");
        Debug.Log(GameManager.Instance.LocalPlayerCore.OwnerClientId);
        Debug.Log(GameManager.Instance.LocalClientId);
    }
}
