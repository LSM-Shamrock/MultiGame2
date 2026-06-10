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
    [SerializeField, ChildrenArrayField] private UI_StatItem[] Stats;

    private CardData _cardData;
    private UnitData _unitData;
    private Attack_MotionData _attack_MotionData;
    private Attack_ProjectileData _attack_ProjectileData;

    private void Start()
    {
        CloseButton.onClick.AddListener(Hide);
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }

    public void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    public void SetCardData(CardData cardData)
    {
        _cardData = cardData;
        _unitData = StaticDB.Instance.UnitData.Dictionary[_cardData.UnitId];

        switch (_unitData.AttackType)
        {

            case AttackType.Motion: _attack_MotionData = StaticDB.Instance.Attack_MotionData.Dictionary[_unitData.AttackId]; break;
            case AttackType.Projectile: _attack_ProjectileData = StaticDB.Instance.Attack_ProjectileData.Dictionary[_unitData.AttackId]; break;
        }

        CardImage.sprite = Resources.Load<Sprite>($"CardSprite/{_cardData.CodeName}");
        MpText.text = $"{_cardData.CostMP}";
        NameText.text = $"{_cardData.DisplayName}";
        DescriptionText.text = $"{_cardData.Description}";

        int i = 0;
        
        Stats[i++].SetDisplay(StatDisplayType.Unit_Health, _unitData);
        Stats[i++].SetDisplay(StatDisplayType.Unit_AltitudeType, _unitData); 
        Stats[i++].SetDisplay(StatDisplayType.Unit_MoveSpeed, _unitData); 
        Stats[i++].SetDisplay(StatDisplayType.Unit_TargetingType, _unitData);
        Stats[i++].SetDisplay(StatDisplayType.Unit_AttackRange, _unitData); 

        switch (_unitData.AttackType)
        {
            case AttackType.Motion:
                Stats[i++].SetDisplay(StatDisplayType.Attack_Motion, _attack_MotionData); 
                break;
            case AttackType.Projectile:
                Stats[i++].SetDisplay(StatDisplayType.Attack_Projectile, _attack_ProjectileData); 
                break;
        }

        for (; i < Stats.Length; i++)
            Stats[i].SetHide(); 
    }
}
