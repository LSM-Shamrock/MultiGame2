using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerCore : NetworkBehaviour
{
    public NetworkVariable<float> MP { get; private set; } = new(4, readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> DeckCardIds { get; private set; } = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> HandCardIds { get; private set; } = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Owner);

    private Queue<int> _nextCardIds = new();

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnPlayerCoreSpawned(this);

        DeckCardIds.OnListChanged += OnDeckCardIdsChanged;
        HandCardIds.OnListChanged += OnHandCardIdsChanged;

        SetupDatas();
    }

    public void SetupDatas()
    {
        if (IsServer)
        {
            var deckCardIds = LobbyManager.Instance.PlayerSessionDatas[OwnerClientId].DeckCardIds;
            foreach (var cardId in deckCardIds) 
                DeckCardIds.Add(cardId);

            SetupHandAndNextCards(deckCardIds);

            StartCoroutine(MpUpdateRoutine());

            Debug.Log("플레이어 데이터 초기 할당됨");
        }
    }

    private void SetupHandAndNextCards(int[] deck)
    {
        for (int i = 0; i < deck.Length; i++)
        {
            int rand = Random.Range(i, deck.Length);

            var temp = deck[i];
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        for (int i = 0; i < deck.Length; i++)
        {
            if (i < 4)
                HandCardIds.Add(deck[i]);
            else
                _nextCardIds.Enqueue(deck[i]);
        }

        Debug.Log("패, 다음 카드들 셋업 완료");
    }

    private IEnumerator MpUpdateRoutine()
    {
        if (!IsServer)
            yield break;

        WaitForSeconds wait = new WaitForSeconds(2f);

        while (true)
        {
            if (MP.Value < 10)
            {
                yield return wait;
                MP.Value += 1f;
            }
            else
            {
                if (MP.Value > 10)
                    MP.Value = 10;

                yield return null;
            }
        }
    }



    private void OnDeckCardIdsChanged(NetworkListEvent<int> changedEvent)
    {
        Debug.Log($"덱 할당됨: {changedEvent.Value}");
    }
    private void OnHandCardIdsChanged(NetworkListEvent<int> changedEvent)
    {
        Debug.Log($"패 할당됨: {changedEvent.Value}");
    }
}
