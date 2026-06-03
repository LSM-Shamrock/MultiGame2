using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    public static GameUI SceneInstance => _sceneInstance != null ? _sceneInstance : (_sceneInstance = FindAnyObjectByType<GameUI>());
    private static GameUI _sceneInstance;

    [SerializeField, ChildField] private GameObject CardSummonArea;
    [SerializeField, ChildField] private CardSummonPosUI CardSummonPos;
    [SerializeField, ChildField] private TextMeshProUGUI LocalPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI OpponentPlayerNameText;
    [SerializeField, ChildField] private MpBarUI MpBar;
    [SerializeField, ChildField] private NextCardUI NextCard;
    [SerializeField, ChildrenGroupField] private HandCardUI[] HandCards;
    [SerializeField, ChildField] private PointerEventBinder DragArea;
    [SerializeField, AssetField("Bgm_Game")] private AudioClip _gameBgm;

    private Camera _camera;
    private Player _player;
    private float _displayMP;
    private int[] _handCardIds = new int[4];
    private bool _isPointerDragArea;
    private int _selectedIndex = -1;
    private int _selectedCardId = -1;
    private CardData _selectedCardData = null;

    private void Start()
    {
        _camera = Camera.main;

        for (int i = 0; i < HandCards.Length; i++)
            HandCards[i].OnPointerDown += OnCardSelect;

        DragArea.AddEvent(PointerEventType.PointerEnter, () => _isPointerDragArea = true);
        DragArea.AddEvent(PointerEventType.PointerExit, () => _isPointerDragArea = false);

        SoundManager.Instance.PlayBgm(_gameBgm);
    }
    private void LateUpdate()
    {
        if (_player == null)
            return;

        if (_displayMP < 10)
            RefreshMP(_displayMP += Time.deltaTime / 2f);
        if (_displayMP > 10)
            RefreshMP(_displayMP = 10);

        if (_selectedCardData != null)
        {
            if (_isPointerDragArea && Input.GetMouseButton(0))
            {
                HandCards[_selectedIndex].SetShow(false);
                CardSummonArea.gameObject.SetActive(true);
                CardSummonPos.gameObject.SetActive(true);
                CardSummonPos.transform.position = _player.WorldToGridPoint(_camera.ScreenToWorldPoint(Input.mousePosition));
            }
            else if (_isPointerDragArea && Input.GetMouseButtonUp(0) && _displayMP >= _selectedCardData.CostMP)
            {
                HandCards[_selectedIndex].SetShow(true);
                HandCards[_selectedIndex].SetSelected(false);
                CardSummonPos.gameObject.SetActive(false);
                _player.SummonCardServerRpc(_selectedIndex, _player.WorldToGridIndex(_camera.ScreenToWorldPoint(Input.mousePosition)));
                _selectedIndex = -1;
                _selectedCardId = -1;
                _selectedCardData = null;
            }
            else
            {
                HandCards[_selectedIndex].SetShow(true);
                CardSummonArea.gameObject.SetActive(false);
                CardSummonPos.gameObject.SetActive(false);
            }
        }
        else
        {
            CardSummonArea.gameObject.SetActive(false);
            CardSummonPos.gameObject.SetActive(false);
        }
    }

    public void SetOpponentPlayer(Player player)
    {
        if (player == null)
            return;

        OpponentPlayerNameText.text = player.PlayerName.Value.ToString();
        player.PlayerName.OnValueChanged += OnOpponentNameChanged;
    }
    public void SetLocalPlayer(Player player)
    {
        if (player == null)
            return;

        _player = player;

        LocalPlayerNameText.text = player.PlayerName.Value.ToString();
        RefreshMP(player.MP.Value);
        RefreshHandCardIds(player.HandCardIds.AsNativeArray());
        RefreshNextCardId(player.NextCardId.Value);

        player.PlayerName.OnValueChanged += OnPlayerNameChanged;
        player.MP.OnValueChanged += OnMpChanged;
        player.HandCardIds.OnListChanged += OnHandCardIdChanged;
        player.NextCardId.OnValueChanged += OnNextCardIdChanged;

        CardSummonArea.transform.rotation = player.transform.rotation;
        CardSummonPos.transform.rotation = player.transform.rotation;
    }

    private void RefreshMP(float value)
    {
        _displayMP = value;

        MpBar.SetMP(_displayMP);

        foreach (var card in HandCards)
            card.SetPlayerMP(_displayMP);

        CardSummonPos.SetPlayerMP(_displayMP);
    }
    private void RefreshNextCardId(int value)
    {
        NextCard.SetCardId(value);
    }
    private void RefreshHandCardIds(NativeArray<int>.ReadOnly values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _handCardIds[i] = values[i];
            HandCards[i].SetCardId(values[i]);
        }
    }

    private void OnCardSelect(int index)
    {
        if (_player == null)
            return;

        _selectedIndex = index;
        _selectedCardId = _handCardIds[_selectedIndex];
        _selectedCardData = StaticDB.Instance.CardData.Dictionary.GetValueOrDefault(_selectedCardId);

        for (int i = 0; i < HandCards.Length; i++)
            HandCards[i].SetSelected(i == _selectedIndex);

        CardSummonPos.SetSelectedHandCardId(_handCardIds[_selectedIndex]);
    }

    #region Network Varriable Changed Callbacks
    private void OnOpponentNameChanged(FixedString32Bytes prev, FixedString32Bytes cur) => OpponentPlayerNameText.text = cur.ToString();
    private void OnPlayerNameChanged(FixedString32Bytes prev, FixedString32Bytes cur) => LocalPlayerNameText.text = cur.ToString();
    private void OnMpChanged(int oldValue, int newValue) => RefreshMP(newValue);
    private void OnNextCardIdChanged(int oldValue, int newValue) => RefreshNextCardId(newValue);
    private void OnHandCardIdChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
            case NetworkListEvent<int>.EventType.Value:
                _handCardIds[changeEvent.Index] = changeEvent.Value;
                HandCards[changeEvent.Index].SetCardId(changeEvent.Value);
                break;
        }
    }
    #endregion
}
