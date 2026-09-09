using UnityEngine;
using KSM._00.Scripts.Crop;
 
/// <summary>
/// 친구의 TimeManager / 계절 시스템과 작물 시스템을 잇는 <b>유일한</b> 다리.
///
/// 작물 쪽 코드는 TimeManager 를 전혀 모른다. 그쪽 클래스 이름이나 enum 이 바뀌면
/// 이 파일 하나만 고치면 된다.
///
/// 씬 배치: TimeManager 가 붙어 있는 오브젝트에 같이 붙이면 편하다.
/// </summary>
public class SeasonGrowthAdapter : MonoBehaviour, IGameClock, IGrowthModifier
{
    [Header("참조")]
    [Tooltip("비워두면 씬에서 자동으로 찾는다")]
    [SerializeField] private TimeManager timeManager;
 
    [Header("계절별 성장 속도 배수")]
    [SerializeField] private float springSpeed = 1.3f;
    [SerializeField] private float summerSpeed = 1.0f;
    [SerializeField] private float autumnSpeed = 1.0f;
    [SerializeField] private float winterSpeed = 0.6f;
 
    [Header("계절별 수확량 배수")]
    [SerializeField] private float springYield = 1.0f;
    [SerializeField] private float summerYield = 1.0f;
    [SerializeField] private float autumnYield = 1.5f;
    [SerializeField] private float winterYield = 1.0f;
 
    [Header("날씨")]
    [Tooltip("비가 오는 동안 성장 속도에 추가로 곱해지는 값")]
    [SerializeField] private float rainSpeedBonus = 1.5f;
 
    [Header("제철")]
    [Tooltip("끄면 제철이 아니어도 아무 작물이나 심을 수 있다")]
    [SerializeField] private bool enforceSeason = true;
 
    /// <summary>
    /// 비가 오는 중인가. SeasonPassive 에서 비를 시작·종료할 때 이 값을 켜고 끄면 된다.
    ///     seasonGrowthAdapter.IsRaining = true;   // 비 시작
    ///     seasonGrowthAdapter.IsRaining = false;  // 비 끝
    /// </summary>
    public bool IsRaining { get; set; }
 
    private void Awake()
    {
        if (timeManager == null) timeManager = FindFirstObjectByType<TimeManager>();
    }
 
    private void OnEnable()
    {
        if (timeManager == null) timeManager = FindFirstObjectByType<TimeManager>();
 
        CropManager mgr = CropManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[계절연동] 씬에서 CropManager 를 찾지 못했습니다.", this);
            return;
        }
 
        // ★ TimeManager 가 없으면 시계로 등록하지 않는다.
        //   등록해버리면 TotalGameDays 가 계속 0이라 작물이 영영 안 자란다.
        if (timeManager != null)
        {
            mgr.GameClock = this;
        }
        else
        {
            Debug.LogWarning(
                "[계절연동] 씬에 TimeManager 가 없어 게임 시계 연동을 건너뜁니다.\n" +
                "작물은 CropManager 의 Fallback Seconds Per Day 로 자랍니다.", this);
        }
 
        // ★ 대입이 아니라 등록이다. 마스터리 같은 다른 보정자와 같이 꽂혀 있을 수 있다
        mgr.AddGrowthModifier(this);
    }
 
    private void OnDisable()
    {
        CropManager mgr = CropManager.Instance;
        if (mgr == null) return;
 
        if (ReferenceEquals(mgr.GameClock, this)) mgr.GameClock = null;
        mgr.RemoveGrowthModifier(this);
    }
 
    // ════════════════════════════════════════════════════════════
    //  IGameClock — 작물이 따라갈 시계
    // ════════════════════════════════════════════════════════════
 
    public float TotalGameDays
    {
        get
        {
            if (timeManager == null) return 0f;
 
            // CurrentHour / CurrentMinute 로 하루 안의 진행률을 낸다
            float withinDay = (timeManager.CurrentHour * 60f + timeManager.CurrentMinute) / 1440f;
            return (timeManager.CurrentDay - 1) + withinDay;
        }
    }
 
    // ════════════════════════════════════════════════════════════
    //  IGrowthModifier — 계절·날씨 보정
    // ════════════════════════════════════════════════════════════
 
    public float GrowthSpeedMultiplier
    {
        get
        {
            if (timeManager == null) return 1f;
 
            float speed = timeManager.CurrentSeason switch
            {
                TimeManager.SeasonPeriod.Spring => springSpeed,
                TimeManager.SeasonPeriod.Summer => summerSpeed,
                TimeManager.SeasonPeriod.Autumn => autumnSpeed,
                _ => winterSpeed,
            };
 
            if (IsRaining) speed *= rainSpeedBonus;
 
            return Mathf.Max(0f, speed);
        }
    }
 
    public float YieldMultiplier
    {
        get
        {
            if (timeManager == null) return 1f;
 
            return timeManager.CurrentSeason switch
            {
                TimeManager.SeasonPeriod.Spring => springYield,
                TimeManager.SeasonPeriod.Summer => summerYield,
                TimeManager.SeasonPeriod.Autumn => autumnYield,
                _ => winterYield,
            };
        }
    }
 
    public float QualityBonus => 0f;   // 나중에 계절/비료 보너스를 넣을 자리
 
    /// <summary>계절은 최상 등급을 막지 않는다. 그 제한은 농사 마스터리가 담당한다</summary>
    public bool AllowBestQuality => true;
 
    public bool CanPlantNow(CropSO crop)
    {
        if (!enforceSeason || crop == null || timeManager == null) return true;
 
        return (crop.plantableSeasons & ToCropSeason(timeManager.CurrentSeason)) != 0;
    }
 
    /// <summary>저쪽 enum 을 이쪽 플래그로 옮기는 유일한 지점</summary>
    private static CropSeason ToCropSeason(TimeManager.SeasonPeriod season) => season switch
    {
        TimeManager.SeasonPeriod.Spring => CropSeason.Spring,
        TimeManager.SeasonPeriod.Summer => CropSeason.Summer,
        TimeManager.SeasonPeriod.Autumn => CropSeason.Autumn,
        _ => CropSeason.Winter,
    };
}