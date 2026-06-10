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

        var hitData = StaticDB.Instance.AttackHitData.Dictionary[data.AttackHitId];

        _nextIndex = 0;
        Stats[_nextIndex++].SetDisplay(StatDisplayType.Attack_Motion_Interval, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit, hitData);

        for (; _nextIndex < Stats.Length; _nextIndex++)
            Stats[_nextIndex].SetHide();
    }
    public void SetDisplay(Attack_ProjectileData data)
    {
        TitleText.text = $"{data.DisplayName}";


        for (; _nextIndex < Stats.Length; _nextIndex++)
            Stats[_nextIndex].SetHide();
    }
    public void SetDisplay(AttackHitData data)
    {
        TitleText.text = $"적중 효과";

        _nextIndex = 0;
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_Damage, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_KnockbackDistance, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_KnockbackSpeed, data);
        Stats[_nextIndex++].SetDisplay(StatDisplayType.AttackHit_DrainRatio, data);

        for (; _nextIndex < Stats.Length; _nextIndex++)
            Stats[_nextIndex].SetHide();
    }
}
