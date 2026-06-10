using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AutoInjectionTarget]
public class UI_StatItem : MonoBehaviour
{
    [SerializeField, ChildField] private Image IconImage;
    [SerializeField, ChildField] private TextMeshProUGUI StatNameText;
    [SerializeField, ChildField] private TextMeshProUGUI StatValueText;
    [SerializeField, ChildField] private Button AdditionalInfoButton;

    public enum DisplayStatType
    {
        None,
        Health,
        AltitudeType,
        TargetingType,
        MoveSpeed,
        AttackRange,
        AttackType,
    }

    private void Awake()
    {
        AdditionalInfoButton.onClick.AddListener(OnAdditionalInfoButtonClick);
    }

    public void SetDisplay(DisplayStatType type, UnitData unitData)
    {
        if (type == DisplayStatType.None || unitData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        AttackData attackData = unitData.AttackType switch
        {
            AttackType.Motion => StaticDB.Instance.Attack_MotionData.Dictionary[unitData.AttackId],
            AttackType.Projectile => StaticDB.Instance.Attack_ProjectileData.Dictionary[unitData.AttackId],
            _ => null
        };

        gameObject.SetActive(true);
        string statName = "";
        string statValue = "";
        bool additionalInfo = false;
        switch (type)
        {
            case DisplayStatType.Health:
                statName = "체력";
                statValue = $"{unitData.Health}";
                break;
            case DisplayStatType.AltitudeType:
                statName = "유형";
                statValue = unitData.AltitudeType switch
                {
                    AltitudeType.Air => "공중",
                    AltitudeType.Ground => "지상",
                    _ => ""
                };
                break;
            case DisplayStatType.TargetingType:
                statName = "공격 대상";
                statValue = unitData.TargetingType switch
                {
                    TargetingType.Ground => "지상",
                    TargetingType.GroundOrAir => "지상 및 공중",
                    TargetingType.Core => "코어",
                    _ => ""
                };
                break;
            case DisplayStatType.MoveSpeed:
                statName = "이동 속도";
                statValue = $"{unitData.MoveSpeed} 타일/s";
                break;
            case DisplayStatType.AttackRange:
                statName = "공격 범위";
                statValue = $"{unitData.AttackRange} 타일";
                break;
            case DisplayStatType.AttackType:
                statName = "공격 유형";
                statValue = $"{attackData.DisplayName}";
                additionalInfo = true;
                break;
        }
        StatNameText.text = statName + ":";
        StatValueText.text = statValue;
        IconImage.sprite = Resources.Load<Sprite>($"StatIconSprite/StatIcon_{type}");
        AdditionalInfoButton.gameObject.SetActive(additionalInfo);
    } 

    private void OnAdditionalInfoButtonClick()
    {
        PopupManager.Instance.ShowPopup<UI_StatAdditionalInfoPopup>().SetDisplay(AdditionalInfoButton.transform.position);
    }
}
