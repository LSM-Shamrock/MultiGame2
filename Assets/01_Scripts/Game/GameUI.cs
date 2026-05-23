using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;
    [SerializeField, ChildrenGroupField] private HandCardUI[] _handCards;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            OnLocalPlayerSpawned(GameManager.Instance.LocalPlayer.Value);
            GameManager.Instance.LocalPlayer.OnValueChanged += OnLocalPlayerSpawned;

            OnOpponentPlayerSpawned(GameManager.Instance.OpponentPlayer.Value);
            GameManager.Instance.OpponentPlayer.OnValueChanged += OnOpponentPlayerSpawned;
        }
    }

    private void OnOpponentPlayerSpawned(Player player)
    {
        if (player == null)
            return;

        player.PlayerName.OnValueChanged += OnOpponentNameChanged;
    }
    private void OnLocalPlayerSpawned(Player player)
    {
        if (player == null)
            return;

        player.PlayerName.OnValueChanged += OnPlayerNameChanged;
        player.MP.OnValueChanged += OnMpChanged;
        player.HandCardIds.OnListChanged += OnHandChanged;

        for (int i = 0; i < player.HandCardIds.Count; i++)
        {
            int cardId = player.HandCardIds[i];
            CardData cardData = StaticDB.Instance.CardDataTable[cardId];
            _handCards[i].SetCardData(cardData);
        }
    }
    
    private void OnPlayerNameChanged(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        _localPlayerNameText.text = cur.ToString();
    }
    private void OnOpponentNameChanged(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        _otherPlayerNameText.text = cur.ToString();
    }

    private void OnMpChanged(float oldValue, float newValue)
    {
        _mpBar.Value = newValue;
    }
    private void OnHandChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                CardData cardData = StaticDB.Instance.CardDataTable[changeEvent.Value];

                Debug.Log($"패 할당됨. 인덱스: {changeEvent.Index}");
                _handCards[changeEvent.Index].SetCardData(cardData);
                break;
        }

        Debug.Log("OnHandChanged");
    }
}
