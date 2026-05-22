using TMPro;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;
    [SerializeField, ChildrenGroupField] private HandCardUI[] _handCards;

    public void Initialize()
    {
        _localPlayerNameText.text = LobbyManager.Instance.PlayerName;
        _otherPlayerNameText.text = LobbyManager.Instance.OpponentPlayerSessionData.PlayerName;

        GameManager.Instance.LocalPlayerCore.MP.OnValueChanged += OnMpChanged;
        GameManager.Instance.LocalPlayerCore.HandCardIds.OnListChanged += OnHandChanged;
    }

    private void OnMpChanged(float oldValue, float newValue)
    {
        _mpBar.Value = newValue;
    }

    private void OnHandChanged(NetworkListEvent<int> changeEvent)
    {
        CardData cardData = StaticDB.Instance.CardDataTable[changeEvent.Value];
        _handCards[changeEvent.Index].SetCardData(cardData);

        Debug.Log("OnHandChanged");
    }
}
