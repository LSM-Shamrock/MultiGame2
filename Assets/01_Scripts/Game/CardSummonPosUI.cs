using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class CardSummonPosUI : MonoBehaviour
{
    [ChildField] public SpriteRenderer UnitSprite;
    [ChildField] public TextMeshProUGUI MpText;
    [ChildField] public TextMeshProUGUI NameText;

    private CardData _cardData;

    public void SetSelectedHandCardId(int cardId)
    {
        if (!StaticDB.Instance.CardDataTable.ContainsKey(cardId))
        {
            _cardData = null;
            gameObject.SetActive(false);
            return;
        }

        _cardData = StaticDB.Instance.CardDataTable[cardId];
        string path = $"UnitSprites/{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        UnitSprite.sprite = sprite;
        NameText.text = _cardData.DisplayName;
    }

    public void SetPlayerMP(float playerMP)
    {
        if (_cardData == null)
            return;

        if (playerMP >= _cardData.CostMP)
        {
            MpText.color = Color.white;
            MpText.text = $"{_cardData.CostMP}";
        }
        else
        {
            MpText.color = Color.red;
            MpText.text = $"{(int)playerMP}/{_cardData.CostMP}";
        }
    }
}
