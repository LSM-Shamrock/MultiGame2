using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _opponentPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;
    [SerializeField, ChildField] private NextCardUI _nextCard;
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

        for (int i = 0; i < player.HandCardIds.Count; i++) 
            _handCards[i].SetCardId(player.HandCardIds[i]);

        _nextCard.SetCardId(player.NextCardId.Value);

        player.PlayerName.OnValueChanged += OnPlayerNameChanged;
        player.MP.OnValueChanged += OnMpChanged;
        player.HandCardIds.OnListChanged += OnHandCardIdChanged;
        player.NextCardId.OnValueChanged += OnNextCardIdChanged;
    }
    
    private void OnOpponentNameChanged(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        _opponentPlayerNameText.text = cur.ToString();
    }
    private void OnPlayerNameChanged(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        _localPlayerNameText.text = cur.ToString();
    }
    private void OnMpChanged(float oldValue, float newValue)
    {
        _mpBar.Value = newValue;
    }
    private void OnHandCardIdChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                _handCards[changeEvent.Index].SetCardId(changeEvent.Value);
                break;
        }

        Debug.Log("OnHandChanged");
    }
    private void OnNextCardIdChanged(int oldValue, int newValue)
    {
        _nextCard.SetCardId(newValue);
    }
}
