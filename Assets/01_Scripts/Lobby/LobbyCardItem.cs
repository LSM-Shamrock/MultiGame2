using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AutoInjectionTarget]
public class LobbyCardItem : MonoBehaviour, IPointerClickHandler
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

        LobbyManager.Instance.CurrentDeck.OnAnyValueChanged += OnChangeDeck;
    }
    private void OnDestroy()
    {
        LobbyManager.Instance.CurrentDeck.OnAnyValueChanged -= OnChangeDeck;
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
        var deck = LobbyManager.Instance.CurrentDeck.Values;
        var collection = StaticDB.Instance.CardDataList;

        if (_isDeck)
        {
            CardData data = deck[_index];

            SetCardData(data);
            SetInteractable(true);
        }
        else
        {
            if (_index >= collection.Count)
            {
                Debug.Log(gameObject);
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            var data = collection[_index];
            var isInDeck = deck.Contains(data);

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
            LobbyManager.Instance.CurrentDeck[_index] = null;
        }
        else
        {
            for (int i = 0; i < LobbyManager.Instance.CurrentDeck.Length; i++)
            {
                if (LobbyManager.Instance.CurrentDeck[i] == null)
                {
                    LobbyManager.Instance.CurrentDeck[i] = _cardData;
                    break;
                }
            }
        }
    }
}
