using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum StatDisplayType
{
    None,
    Unit_Health,
    Unit_AltitudeType,
    Unit_TargetingType,
    Unit_MoveSpeed,
    Unit_AttackRange,
    Unit_BackoffRatio,
    Unit_BackoffSpeedRatio,
    AttackHit,
    AttackHit_Damage,
    AttackHit_KnockbackDistance,
    AttackHit_KnockbackSpeed,
    AttackHit_DrainRatio,
    DotEffect_Damage,
    DotEffect_Interval,
    Attack_Motion,
    Attack_Motion_Interval,
    Attack_Projectile,
    Attack_Projectile_Interval,
    Projectile_Speed,
    Projectile_MaxDistance,
}

[AutoInjectionTarget]
public class UI_StatItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, ChildField] private Image IconImage;
    [SerializeField, ChildField] private TextMeshProUGUI StatNameText;
    [SerializeField, ChildField] private TextMeshProUGUI StatValueText;
    [SerializeField, ChildField] private Button AdditionalInfoButton;
    [SerializeField, ChildField] private Transform AdditionalInfoPos;

    private StatDisplayType _type;
    private object _data;
    private bool _hasAdditionalInfo;

    private void Awake()
    {
        AdditionalInfoButton.onClick.AddListener(OnAdditionalInfoButtonClick);
    }

    private void SetDiplay(StatDisplayType type, object data, string statName, string statValue, bool hide, bool hasAdditionalInfo)
    {
        _type = type;
        _data = data;
        _hasAdditionalInfo = hasAdditionalInfo;

        bool active = type != StatDisplayType.None && data != null && !hide;
        gameObject.SetActive(active);
        if (!active) return;

        StatNameText.text = statName + ":";
        StatValueText.text = statValue;
        
        var iconSprite = Resources.Load<Sprite>($"StatIconSprite/StatIcon_{type}");
        IconImage.sprite = iconSprite;
        IconImage.gameObject.SetActive(iconSprite != null);

        AdditionalInfoButton.gameObject.SetActive(hasAdditionalInfo);
    }
    public void SetHide()
    {
        SetDiplay(StatDisplayType.None, null, "", "", true, false);
    }
    public void SetDisplay(StatDisplayType type, UnitData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.Unit_Health:
                statName = "체력";
                statValue = $"{data.Health}";
                break;
            case StatDisplayType.Unit_AltitudeType:
                statName = "유형";
                statValue = data.AltitudeType switch
                {
                    AltitudeType.Air => "공중",
                    AltitudeType.Ground => "지상",
                    _ => ""
                };
                break;
            case StatDisplayType.Unit_TargetingType:
                statName = "공격 대상";
                statValue = data.TargetingType switch
                {
                    TargetingType.Ground => "지상",
                    TargetingType.GroundOrAir => "지상 및 공중",
                    TargetingType.Core => "코어",
                    _ => ""
                };
                break;
            case StatDisplayType.Unit_MoveSpeed:
                statName = "이동 속도";
                statValue = $"{data.MoveSpeed} 타일/초";
                break;
            case StatDisplayType.Unit_AttackRange:
                statName = "공격 범위";
                statValue = $"{data.AttackRange} 타일";
                break;
            case StatDisplayType.Unit_BackoffRatio:
                statName = "백무빙 거리";
                statValue = $"공격 범위의 {data.BackoffRatio * 100}%";
                hide = data.BackoffRatio == 0;
                break;
            case StatDisplayType.Unit_BackoffSpeedRatio:
                statName = "백무빙 속도";
                statValue = $"기본 속도의 {data.BackoffSpeedRatio * 100}%";
                hide = data.BackoffRatio == 0;
                break;
        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }
    public void SetDisplay(StatDisplayType type, Attack_MotionData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.Attack_Motion:
                statName = "선판정 공격";
                statValue = $"{data.DisplayName}";
                additionalInfo = true;
                break;
            case StatDisplayType.Attack_Motion_Interval:
                statName = "공격 간격";
                statValue = $"{data.MotionTime + data.Cooltime}초";
                break;

        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }
    public void SetDisplay(StatDisplayType type, Attack_ProjectileData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.Attack_Projectile:
                statName = "투사체 공격";
                statValue = $"{data.DisplayName}";
                additionalInfo = true;
                break;
            case StatDisplayType.Attack_Projectile_Interval:
                statName = "발사 간격";
                statValue = $"{data.Cooltime}초";
                break;
        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }
    public void SetDisplay(StatDisplayType type, ProjectileData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.Projectile_Speed:
                statName = $"{data.DisplayName} 발사 속도";
                statValue = $"{data.Speed} 타일/초";
                break;
            case StatDisplayType.Projectile_MaxDistance:
                statName = $"{data.DisplayName} 최대 발사 거리";
                statValue = $"{data.MaxDistance} 타일";
                break;
        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }
    public void SetDisplay(StatDisplayType type, AttackHitData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.AttackHit:
                statName = "적중 효과";
                statValue = $">";
                additionalInfo = true;
                break;
            case StatDisplayType.AttackHit_Damage:
                statName = "피해량";
                statValue = $"{data.Damage}";
                break;
            case StatDisplayType.AttackHit_KnockbackDistance:
                statName = "밀치기 거리";
                statValue = $"{data.KnockbackDistance} 타일";
                hide = data.KnockbackDistance == 0;
                break;
            case StatDisplayType.AttackHit_KnockbackSpeed:
                statName = "밀치기 속도";
                statValue = $"{data.KnockbackSpeed} 타일/초";
                hide = data.KnockbackDistance == 0;
                break;
            case StatDisplayType.AttackHit_DrainRatio:
                statName = "흡혈 비율";
                statValue = $"{data.DrainRatio * 100}%";
                hide = data.DrainRatio == 0;
                break;

        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }
    public void SetDisplay(StatDisplayType type, DotEffectData data)
    {
        string statName = "";
        string statValue = "";
        bool hide = false;
        bool additionalInfo = false;
        switch (type)
        {
            case StatDisplayType.DotEffect_Damage:
                statName = $"{data.DisplayName} 효과 피해량";
                statValue = $"{data.DotDamage} x {data.DotCount}";
                break;
            case StatDisplayType.DotEffect_Interval:
                statName = $"{data.DisplayName} 효과 간격";
                statValue = $"{data.DotInterval} 초";
                break;

        }
        SetDiplay(type, data, statName, statValue, hide, additionalInfo);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        OnAdditionalInfoButtonClick();
    }
    private void OnAdditionalInfoButtonClick()
    {
        if (_hasAdditionalInfo == false)
            return;

        var p = AdditionalInfoPos.position;
        
        var ui = PopupManager.Instance.ShowPopup<UI_StatAdditionalInfoPopup>();
        ui.SetPosition(p);

        switch (_type)
        {
            case StatDisplayType.Attack_Motion: ui.SetDisplay(_data as Attack_MotionData); break;
            case StatDisplayType.Attack_Projectile: ui.SetDisplay(_data as Attack_ProjectileData); break;
            default: ui.Hide(); break;
        }
    }
}
