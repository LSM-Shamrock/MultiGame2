using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;

    private void Update()
    {
        var datas = GameManager.Instance.PlayerSessionDatas;
        _localPlayerNameText.text = datas[GameManager.Instance.LocalPlayerNumber].PlayerName;
        _otherPlayerNameText.text = datas[GameManager.Instance.OtherPlayerNumber].PlayerName;
    }
}
