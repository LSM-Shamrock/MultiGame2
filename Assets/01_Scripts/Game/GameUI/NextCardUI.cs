using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class NextCardUI : MonoBehaviour
{
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;

    public void SetCardId(int cardId)
    {
        CardImage.gameObject.SetActive(cardId != -1);

        if (!StaticDB.Instance.CardData.Dictionary.ContainsKey(cardId))
            return;

        CardData cardData = StaticDB.Instance.CardData.Dictionary[cardId];

        string path = $"CardSprite/{cardData.CodeName}";
        Sprite sprite = Resources.Load<Sprite>(path);

        CardImage.sprite = sprite;
        MpText.text = $"{cardData.CostMP}";
    }
}
