using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI LocalPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI OpponentPlayerNameText;
    [SerializeField, ChildField] private MpBarUI MpBar;
    [SerializeField, ChildField] private NextCardUI NextCard;
    [SerializeField, ChildrenGroupField] private HandCardUI[] HandCards;
    [SerializeField, ChildField] private PointerEventHandler DragArea;

    private float MP
    {
        get => _mp;
        set
        {
            _mp = value;

            MpBar.SetMP(_mp);
            foreach (var card in HandCards)
                card.SetPlayerMP(_mp);
        }
    }
    private float _mp;

    private int _selectedIndex = -1;

    private void Start()
    {
        for (int i = 0; i < HandCards.Length; i++)
            HandCards[i].OnPointerDown += OnPointerDown_HandCard;

        DragArea.OnPointerEnter += OnPointerEnter_DragArea;
        DragArea.OnPointerExit += OnPointerExit_DragArea;

        if (GameScene.Instance)
        {
            OnChanged_LocalPlayer(GameScene.Instance.LocalPlayer.Value);
            GameScene.Instance.LocalPlayer.OnValueChanged += OnChanged_LocalPlayer;

            OnChanged_OpponentPlayer(GameScene.Instance.OpponentPlayer.Value);
            GameScene.Instance.OpponentPlayer.OnValueChanged += OnChanged_OpponentPlayer;
        }
    }

    private void LateUpdate()
    {
        if (MP < 10)
            MP += Time.deltaTime / 2f;
        if (MP > 10)
            MP = 10;

        if (Input.GetMouseButtonUp(0))
            OnMouseButtonUp();
    }

    private void OnChanged_OpponentPlayer(Player player)
    {
        if (player == null)
            return;

        player.PlayerName.OnValueChanged += OnChanged_OpponentPlayerName;
    }
    private void OnChanged_OpponentPlayerName(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        OpponentPlayerNameText.text = cur.ToString();
    }
    
    private void OnChanged_LocalPlayer(Player player)
    {
        if (player == null)
            return;

        for (int i = 0; i < player.HandCardIds.Count; i++)
            HandCards[i].SetCardId(player.HandCardIds[i]);

        NextCard.SetCardId(player.NextCardId.Value);

        player.PlayerName.OnValueChanged += OnChanged_LocalPlayerName;
        player.MP.OnValueChanged += OnChanged_LocalPlayerMP;
        player.HandCardIds.OnListChanged += OnChanged_LocalPlayerHandCardId;
        player.NextCardId.OnValueChanged += OnChanged_LocalPlayerNextCardId;
    }
    private void OnChanged_LocalPlayerName(FixedString32Bytes prev, FixedString32Bytes cur)
    {
        LocalPlayerNameText.text = cur.ToString();
    }
    private void OnChanged_LocalPlayerMP(int oldValue, int newValue)
    {
        MP = newValue;
    }
    private void OnChanged_LocalPlayerHandCardId(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                HandCards[changeEvent.Index].SetCardId(changeEvent.Value);
                break;
        }
    }
    private void OnChanged_LocalPlayerNextCardId(int oldValue, int newValue)
    {
        NextCard.SetCardId(newValue);
    }
    
    private void OnPointerDown_HandCard(int index)
    {
        _selectedIndex = index;

        for (int i = 0; i < HandCards.Length; i++)
            HandCards[i].SetSelected(i == _selectedIndex);
    }
    private void OnPointerEnter_DragArea(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
            if (_selectedIndex != -1)
            {
                HandCards[_selectedIndex].SetHide(true);
            }
        }
    }
    private void OnPointerExit_DragArea(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
            if (_selectedIndex != -1)
            {
                HandCards[_selectedIndex].SetHide(false);
            }
        }
    }
    private void OnMouseButtonUp()
    {
        if (_selectedIndex != -1)
        {
            HandCards[_selectedIndex].SetHide(false);
        }
    }
}
