using TMPro;
using UnityEngine;

[AutoInjectionTarget]
public class CardSummonPosUI : MonoBehaviour
{
    [ChildField] public SpriteRenderer UnitSprite;
    [ChildField] public TextMeshProUGUI MpText;
    [ChildField] public TextMeshProUGUI NameText;

    private CardData _cardData;
    private UnitData _unitData;
    private Sprite _sprite;
    private float _playerMP;

    public void SetSelectedHandCardId(int cardId)
    {
        if (!StaticDB.Instance.CardData.Dictionary.ContainsKey(cardId))
        {
            _cardData = null;
            return;
        }

        _cardData = StaticDB.Instance.CardData.Dictionary[cardId];
        _unitData = StaticDB.Instance.UnitData.Dictionary[_cardData.UnitId];

        string path = $"UnitSprite/{_unitData.CodeName}";
        _sprite = Resources.Load<Sprite>(path);
        Refresh();
    }
    public void SetPlayerMP(float playerMP)
    {
        _playerMP = playerMP;
        Refresh();
    }

    private void Refresh()
    {
        if (_cardData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            UnitSprite.sprite = _sprite;
            UnitSprite.transform.localPosition = new Vector3(0, _unitData.SummonHeight);
            NameText.text = _cardData.DisplayName;

            if (_playerMP >= _cardData.CostMP)
            {
                MpText.color = Color.white;
                MpText.text = $"{_cardData.CostMP}";
            }
            else
            {
                MpText.color = Color.red;
                MpText.text = $"{(int)_playerMP}/{_cardData.CostMP}";
            }
        }
    }
}
