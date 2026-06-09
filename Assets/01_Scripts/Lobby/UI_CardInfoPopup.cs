using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_CardInfoPopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField] private PointerEventBinder BackPanel;
    [SerializeField, ChildField] private Button CloseButton;
    [SerializeField, ChildField] private Image CardImage;
    [SerializeField, ChildField] private TextMeshProUGUI MpText;
    [SerializeField, ChildField] private TextMeshProUGUI NameText;
    [SerializeField, ChildField] private TextMeshProUGUI DescriptionText;
    [SerializeField, ChildrenGroupField] private UI_CardInfoStat[] Stats;

    private CardData _cardData;
    private UnitData _unitData;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }

    private void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    public void SetCardData(CardData cardData)
    {
        _cardData = cardData;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_cardData.UnitId];

        CardImage.sprite = Resources.Load<Sprite>($"CardSprite/{_cardData.CodeName}");
        MpText.text = $"{_cardData.CostMP}";
        NameText.text = $"{_cardData.DisplayName}";
        DescriptionText.text = $"{_cardData.Description}";

        for (int i = 0; i < Stats.Length; i++)
        {
            var stat = Stats[i];
            switch (i)
            {
                case 0: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.Health, _unitData); break;
                case 1: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.AltitudeType, _unitData); break;
                case 2: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.MoveSpeed, _unitData); break;
                case 3: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.TargetingType, _unitData); break;
                case 4: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.AttackRange, _unitData); break;
                case 5: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.AttackType, _unitData); break;
                default: stat.SetDisplay(UI_CardInfoStat.DisplayStatType.None, _unitData); break;   
            }
        }
    }
}
