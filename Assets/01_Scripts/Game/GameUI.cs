using TMPro;
using Unity.Netcode;
using UnityEngine;

[AutoInjectionTarget]
public class GameUI : MonoBehaviour
{
    [SerializeField, ChildField] private TextMeshProUGUI _localPlayerNameText;
    [SerializeField, ChildField] private TextMeshProUGUI _otherPlayerNameText;
    [SerializeField, ChildField] private MpBarUI _mpBar;
    [SerializeField, ChildrenGroupField] private HandCardUI[] _handCards;


    private void OnMpChanged(float oldValue, float newValue)
    {
        _mpBar.Value = newValue;
    }

    private void OnHandChanged(NetworkListEvent<int> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<int>.EventType.Add:
                CardData cardData = StaticDB.Instance.CardDataTable[changeEvent.Value];

                Debug.Log($"패 할당됨. 인덱스: {changeEvent.Index}");
                _handCards[changeEvent.Index].SetCardData(cardData);
                break;
        }

        Debug.Log("OnHandChanged");
    }

    private void RefreshHandCards(int[] handCardIds)
    {
        for (int i = 0; i < handCardIds.Length; i++)
        {
            int cardId = handCardIds[i];
            CardData cardData = StaticDB.Instance.CardDataTable[cardId];
            _handCards[i].SetCardData(cardData);
        }
    }
}
