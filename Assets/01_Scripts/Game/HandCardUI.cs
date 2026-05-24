using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AutoInjectionTarget]
public class HandCardUI : MonoBehaviour
{
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private Image FadeImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;

    private CardData _cardData;

    public void SetCardId(int cardId)
    {
        CardImage.gameObject.SetActive(cardId != -1);

        if (!StaticDB.Instance.CardDataTable.ContainsKey(cardId))
            return;

        _cardData = StaticDB.Instance.CardDataTable[cardId];
        string path = $"CardSprites/{_cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        CardImage.sprite = sprite;
        MpText.text = $"{_cardData.CostMP}";
    }

    public void SetPlayerMP(float playerMP)
    {
        if (_cardData == null)
            return;

        if (playerMP >= _cardData.CostMP)
        {
            FadeImage.fillAmount = 0f;
            CardImage.color = Color.white;
            MpText.color = Color.white;
        }
        else
        {
            FadeImage.fillAmount =  1 - playerMP / _cardData.CostMP;
            CardImage.color = Color.gray;
            MpText.color = Color.red;
        }
    }
}
