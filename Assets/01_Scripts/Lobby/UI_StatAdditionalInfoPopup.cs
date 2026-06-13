using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static UnityEngine.Analytics.IAnalytic;

[AutoInjectionTarget]
public class UI_StatAdditionalInfoPopup : MonoBehaviour, IPopupUI
{
    Canvas IPopupUI.Canvas => Canvas;
    GameObject IPopupUI.GameObject => gameObject;

    [SerializeField, ComponentField] private Canvas Canvas;
    [SerializeField, ChildField("BackPanel")] private PointerEventBinder BackPanel;
    [SerializeField, ChildField("Tail")] private RectTransform Tail;
    [SerializeField, ChildField("TitleText")] private TextMeshProUGUI TitleText;
    [SerializeField, ChildrenArrayField("Box")] private UI_StatItem[] Stats;

    private StatDisplayType _type;
    private object _data;
    private int _nextIndex;

    private void Start()
    {
        BackPanel.AddEvent(PointerEventType.PointerClick, Hide);
    }

    public void Hide()
    {
        PopupManager.Instance.ClosePopup(this);
    }

    public void SetPosition(Vector2 position) 
    {
        Tail.position = position;
    }

    public void SetDisplay(Attack_MotionData data)
    {
        TitleText.text = $"{data.DisplayName}";

        AttackHitData hitData = RemoteConfigManager.Instance.GameData.Value.AttackHitData.Dictionary[data.AttackHitId];

        _nextIndex = 0;
        Stats[_nextIndex++].SetDisplay(StatDisplayType.Attack_Motion_Interval, data);
        AddAttackHitDatas(hitData);

        for (; _nextIndex < Stats.Length; _nextIndex++)
            Stats[_nextIndex].SetHide();
    }
    public void SetDisplay(Attack_ProjectileData data)
    {
        TitleText.text = $"{data.DisplayName}";

        ProjectileData projectileData = RemoteConfigManager.Instance.GameData.Value.ProjectileData.Dictionary[data.ProjectileId];
        AttackHitData hitData = RemoteConfigManager.Instance.GameData.Value.AttackHitData.Dictionary[projectileData.AttackHitId];

        _nextIndex = 0;
        Stats[_nextIndex++].SetDisplay(StatDisplayType.Attack_Projectile_Interval, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.Projectile_Speed, projectileData);
        AddAttackHitDatas(hitData);

        for (; _nextIndex < Stats.Length; _nextIndex++)
            Stats[_nextIndex].SetHide();
    }

    private void AddAttackHitDatas(AttackHitData data)
    {
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_Damage, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_KnockbackDistance, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_KnockbackSpeed, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_DrainRatio, data);

        if (data.DotEffectId != 0)
        {
            DotEffectData dotEffectData = RemoteConfigManager.Instance.GameData.Value.DotEffectData.Dictionary[data.DotEffectId];

            Stats[_nextIndex++].SetDisplay(StatDisplayType.DotEffect_Damage, dotEffectData);
            Stats[_nextIndex++].SetDisplay(StatDisplayType.DotEffect_Interval, dotEffectData);
        }
    }
}
