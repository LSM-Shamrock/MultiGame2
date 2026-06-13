using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_HandCard : MonoBehaviour, IPointerDownHandler
{
    [SerializeField, ChildField("CardImage")] private Image CardImage;
    [SerializeField, ChildField("CardImage")] private Animator Animator;
    [SerializeField, ChildField("FadeImage")] private Image FadeImage;
    [SerializeField, ChildField("MpText")] private TextMeshProUGUI MpText;

    private CardData _cardData;

    public event Action<int> OnPointerDown;

    public void SetCardId(int cardId)
    {
        if (!RemoteConfigManager.Instance.GameData.Value.CardData.Dictionary.ContainsKey(cardId))
        {
            _cardData = null;
            CardImage.gameObject.SetActive(false);
            return;
        }

        CardImage.gameObject.SetActive(true);

        _cardData = RemoteConfigManager.Instance.GameData.Value.CardData.Dictionary[cardId];
        string path = $"CardSprite/{_cardData.CodeName}";
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
            FadeImage.fillAmount = 1 - playerMP / _cardData.CostMP;
            CardImage.color = Color.gray;
            MpText.color = Color.red;
        }

        Animator.SetBool("IsMpMax", playerMP == 10);
    }
    public void SetSelected(bool isSelected)
    {
        CardImage.transform.localPosition = new Vector3(0, isSelected ? 30f : 0f);
    }
    public void SetShow(bool isShow)
    {
        CardImage.gameObject.SetActive(isShow);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        OnPointerDown?.Invoke(transform.GetSiblingIndex());
    }
}
