using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

[AutoInjectionTarget]
public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName { get; private set; } = new();
    public NetworkVariable<int> MP { get; private set; } = new(4, readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> DeckCardIds { get; private set; } = new(readPerm: NetworkVariableReadPermission.Owner);
    public NetworkList<int> HandCardIds { get; private set; } = new(readPerm: NetworkVariableReadPermission.Owner);
    public NetworkVariable<int> NextCardId { get; private set; } = new(readPerm: NetworkVariableReadPermission.Owner);

    private string _playerName;
    private int[] _deckCardIds;
    private Queue<int> _nextCardIds = new();
    private Player _opponent;

    [SerializeField, ChildField] private Transform CorePos;
    [SerializeField, ChildrenGroupField] private Transform[] SummonGrid;
    [SerializeField, AssetField("Unit")] private GameObject _unitPrefab;
    [SerializeField, AssetField("Core")] private GameObject _corePrefab;

    public Core Core { get; private set; }
    public HashSet<Unit> GroundUnits { get; } = new();
    public HashSet<Unit> AllUnits { get; } = new();
    public HashSet<FieldObject> AllObjects { get; } = new();
    public bool IsDead { get; set; } = false;

    public void Init(string playerName, int[] deckCardIds)
    {
        _playerName = playerName;
        _deckCardIds = deckCardIds;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlayerName.Value = _playerName ?? "";

            foreach (var cardId in _deckCardIds)
                DeckCardIds.Add(cardId);

            SummonCore();
            SetupHandAndNextCards(_deckCardIds);
            StartCoroutine(MpUpdateRoutine());
        }

        if (IsOwner)
        {
            Camera.main.transform.rotation = transform.rotation;
            GameScene.SceneInstance.LocalPlayer.Value = this;
            GameUI.SceneInstance.SetLocalPlayer(this);
        }
        else
        {
            GameScene.SceneInstance.OpponentPlayer.Value = this;
            GameUI.SceneInstance.SetOpponentPlayer(this);
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
                MP.Value++;
            }
            else
            {
                if (MP.Value > 10)
                    MP.Value = 10;

                yield return null;
            }
        }
    }

    [ServerRpc]
    public void SummonCardServerRpc(int handIndex, int gridIndex)
    {
        Debug.Log("카드 소환 요청 RPC호출됨");

        if (handIndex < 0 || handIndex > HandCardIds.Count - 1) return;
        if (gridIndex < 0 || gridIndex > SummonGrid.Length - 1) return;

        int handCardId = HandCardIds[handIndex];
        CardData cardData = StaticDB.Instance.CardData.Dictionary[handCardId];

        if (MP.Value < cardData.CostMP)
        {
            Debug.Log("MP가 부족하여 유닛 소환 안함");
            return;
        }
        else
        {
            MP.Value -= cardData.CostMP;

            _nextCardIds.Enqueue(handCardId);
            HandCardIds[handIndex] = _nextCardIds.Dequeue();
            NextCardId.Value = _nextCardIds.Peek();

            UnitData unitData = StaticDB.Instance.UnitData.Dictionary[cardData.UnitId];
            Vector3 position = SummonGrid[gridIndex].position;
            position.y += unitData.SummonHeight;
            SummonUnit(unitData, position);
        }
    }
    public Vector2 WorldToGridPoint(Vector2 worldPos)
    {
        float nearest = float.PositiveInfinity;
        Vector2 result = Vector2.zero;

        foreach (Transform t in SummonGrid)
        {
            Vector2 p = (Vector2)t.position;
            float dist = Math.Abs(p.x - worldPos.x);

            if (dist < nearest)
            {
                nearest = dist;
                result = p;
            }
        }

        return result;
    }
    public int WorldToGridIndex(Vector2 worldPos)
    {
        float nearest = float.PositiveInfinity;
        int result = 0;
        

        for (int i = 0; i < SummonGrid.Length; i++)
        {
            Vector2 p = (Vector2)SummonGrid[i].position;
            float dist = Math.Abs(p.x - worldPos.x);

            if (dist < nearest)
            {
                nearest = dist;
                result = i;
            }
        }

        return result;
    }

    private void SummonCore()
    {
        GameObject go = Instantiate(_corePrefab, (Vector2)CorePos.position, CorePos.rotation);
        Core = go.GetComponent<Core>();

        Core.Init(this);
        Core.NetworkObject.SpawnWithOwnership(OwnerClientId);

        AllObjects.Add(Core);
    }
    private void SummonUnit(UnitData unitData, Vector2 position)
    {
        GameObject go = Instantiate(_unitPrefab, position, Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();

        Player opponent = IsOwner ? GameScene.SceneInstance.OpponentPlayer.Value : GameScene.SceneInstance.LocalPlayer.Value;

        unit.Init(unitData.UnitId, this, opponent);
        unit.NetworkObject.SpawnWithOwnership(OwnerClientId);
    }
}
