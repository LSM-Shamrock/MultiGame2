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
    [SerializeField, ChildField] private TextMeshProUGUI MpText;

    public void SetCardId(int cardId)
    {
        CardImage.gameObject.SetActive(cardId != -1);

        if (!StaticDB.Instance.CardDataTable.ContainsKey(cardId))
            return;

        CardData cardData = StaticDB.Instance.CardDataTable[cardId];
        string path = $"CardSprites/{cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        CardImage.sprite = sprite;
        MpText.text = $"{cardData.CostMP}";
    }
}
