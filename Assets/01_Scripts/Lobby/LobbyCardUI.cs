using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyCardUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private Image FadeImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;

    private CardData _cardData;
    private int _index;
    private bool _isDeck;
    private bool _isInteractable;

    private void Start()
    {
        _index = transform.GetSiblingIndex();
        _isDeck = transform.parent.name == "Deck";

        OnChangeDeck();

        if (GameManager.Instance != null)
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged += OnChangeDeck;
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) 
            GameManager.Instance.CurrentDeckCardIds.OnAnyValueChanged -= OnChangeDeck;
    }

    private void SetCardData(CardData cardData)
    {
        _cardData = cardData;

        CardImage.gameObject.SetActive(cardData != null);

        if (cardData == null)
            return;

        var path = $"CardSprites/{cardData.CodeName}";
        var sprite = Resources.Load<Sprite>(path);
        CardImage.sprite = sprite;

        MpText.text = $"{cardData.CostMP}";
    }
    private void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
        CardImage.raycastTarget = isInteractable;
        FadeImage.gameObject.SetActive(!isInteractable);
    }
    
    private void OnChangeDeck()
    {

        if (_isDeck)
        {
            var deckCardIds = GameManager.Instance.CurrentDeckCardIds.Values;
            int cardId = deckCardIds[_index];

            CardData cardData = StaticDB.Instance.CardDataTable.GetValueOrDefault(cardId);

            SetCardData(cardData);
            SetInteractable(true);
        }
        else
        {
            var deckCardIds = GameManager.Instance.CurrentDeckCardIds.Values;
            var collection = StaticDB.Instance.CardDataList;


            if (_index >= collection.Count)
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            CardData data = collection[_index];
            bool isInDeck = deckCardIds.Contains(data.CardId);

            SetCardData(data);
            SetInteractable(!isInDeck);
        }
}

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractable)
            return;

        if (_isDeck)
        {
            GameManager.Instance.CurrentDeckCardIds[_index] = -1;
        }
        else
        {
            for (int i = 0; i < GameManager.Instance.CurrentDeckCardIds.Length; i++)
            {
                if (GameManager.Instance.CurrentDeckCardIds[i] == -1)
                {
                    GameManager.Instance.CurrentDeckCardIds[i] = _cardData.CardId;
                    break;
                }
            }
        }
    }
}
