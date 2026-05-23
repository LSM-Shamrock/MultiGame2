using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName { get; private set; } = new NetworkVariable<FixedString32Bytes>();
    public NetworkVariable<float> MP { get; private set; } = new(4, readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> DeckCardIds { get; private set; } = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> HandCardIds { get; private set; } = new NetworkList<int>(readPerm: NetworkVariableReadPermission.Owner);
    public NetworkVariable<int> NextCardId { get; private set; } = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Owner);

    private string _playerName;
    private int[] _deckCardIds;
    private Queue<int> _nextCardIds = new();

    public void Init(string playerName, int[] deckCardIds)
    {
        _playerName = playerName;
        _deckCardIds = deckCardIds;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            GameManager.Instance.LocalPlayer.Value = this;
        else
            GameManager.Instance.OpponentPlayer.Value = this;

        if (IsServer)
        {
            PlayerName.Value = _playerName ?? "";

            foreach (var cardId in _deckCardIds)
                DeckCardIds.Add(cardId);

            SetupHandAndNextCards(_deckCardIds);

            StartCoroutine(MpUpdateRoutine());

            Debug.Log("플레이어 데이터 초기 할당됨");
        }
    }

    private void SetupHandAndNextCards(int[] deck)
    {
        int[] shuffled = new int[deck.Length];
        Array.Copy(deck, shuffled, deck.Length);

        for (int i = 0; i < shuffled.Length; i++)
        {
            int rand = UnityEngine.Random.Range(i, shuffled.Length);

            var temp = shuffled[i];
            shuffled[i] = shuffled[rand];
            shuffled[rand] = temp;
        }

        for (int i = 0; i < shuffled.Length; i++)
        {
            if (i < 4)
                HandCardIds.Add(shuffled[i]);
            else
                _nextCardIds.Enqueue(shuffled[i]);
        }

        NextCardId.Value = _nextCardIds.Peek();

        Debug.Log("패, 다음 카드들 셋업 완료");
        Debug.Log("다음 카드 Id : " + NextCardId.Value);
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
}
