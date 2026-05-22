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

    public void SetCardData(CardData cardData)
    {
        Debug.Log("SetCardData 호출됨");
        CardImage.gameObject.SetActive(cardData != null);

        if (cardData == null)
            return;

        var path = $"CardSprites/{cardData.CodeName}";
        var sprite = Resources.Load<Sprite>(path);
        CardImage.sprite = sprite;

        MpText.text = $"{cardData.CostMP}";
    }
}
