using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_LobbyCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;
    [SerializeField, ChildField] private GameObject InUseDisplay;
    [SerializeField, ChildField] private GameObject SelectMenuDisplay;
    [SerializeField, ChildField] private Button InfoButton;
    [SerializeField, ChildField] private Button AppendButton;
    [SerializeField, ChildField] private Button RemoveButton;

    private CardData _cardData;
    private int _index;
    private bool _isDeck;
    private bool _isInDeck;
    private bool _isOnPointer;

    private void Awake()
    {
        _index = transform.GetSiblingIndex();
        _isDeck = transform.parent.name == "Deck";
        SetIsInDeck(_isDeck);

        InfoButton.onClick.AddListener(OnInfoButtonClick);
        AppendButton.onClick.AddListener(OnAppendButtonClick);
        RemoveButton.onClick.AddListener(OnRemoveButtonClick);

        if (GameManager.Instance != null)
        {
            OnChangeDeck(GameManager.Instance.CurrentDeckCardIds.Values);
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged += OnChangeDeck;
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged -= OnChangeDeck;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            SelectMenuDisplay.SetActive(_isOnPointer);
    }

    private void OnChangeDeck(IReadOnlyList<int> deckCardIds)
    {
        if (_isDeck)
        {
            int cardId = deckCardIds[_index];
            CardData cardData = StaticDB.Instance.CardData.Dictionary.GetValueOrDefault(cardId);
            SetCardData(cardData);
        }
        else
        {
            var collection = StaticDB.Instance.CardData.List;

            if (_index >= collection.Count)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            CardData data = collection[_index];
            bool isInDeck = deckCardIds.Contains(data.CardId);

            SetCardData(data);
            SetIsInDeck(isInDeck);
        }
    }

    private void SetCardData(CardData cardData)
    {
        _cardData = cardData;

        CardImage.gameObject.SetActive(cardData != null);

        if (cardData == null)
            return;

        var path = $"CardSprite/{cardData.CodeName}";
        var sprite = Resources.Load<Sprite>(path);
        CardImage.sprite = sprite;

        MpText.text = $"{cardData.CostMP}";
    }
    private void SetIsInDeck(bool isInDeck)
    {
        _isInDeck = isInDeck;
        InUseDisplay.gameObject.SetActive(_isInDeck && !_isDeck);
        AppendButton.gameObject.SetActive(!_isInDeck);
        RemoveButton.gameObject.SetActive(_isInDeck);
    }

    private void OnInfoButtonClick()
    {
        SelectMenuDisplay.SetActive(false);

        PopupManager.Instance.ShowPopup<UI_CardInfoPopup>();
    }
    private void OnAppendButtonClick()
    {
        SelectMenuDisplay.SetActive(false);

        for (int i = 0; i < GameManager.Instance.CurrentDeckCardIds.Length; i++)
        {
            if (GameManager.Instance.CurrentDeckCardIds[i] == -1)
            {
                GameManager.Instance.CurrentDeckCardIds[i] = _cardData.CardId;
                break;
            }
        }
    }
    private void OnRemoveButtonClick()
    {
        SelectMenuDisplay.SetActive(false);

        for (int i = 0; i < GameManager.Instance.CurrentDeckCardIds.Length; i++)
        {
            if (GameManager.Instance.CurrentDeckCardIds[i] == _cardData.CardId)
            {
                GameManager.Instance.CurrentDeckCardIds[i] = -1;
                break;
            }
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        _isOnPointer = true;
    }
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _isOnPointer = false;
    }
}
