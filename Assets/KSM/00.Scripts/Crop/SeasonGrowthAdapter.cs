using UnityEngine;
using KSM._00.Scripts.Crop;
 
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
 
        mgr.AddGrowthModifier(this);
    }
 
    private void OnDisable()
    {
        CropManager mgr = CropManager.Instance;
        if (mgr == null) return;
 
        if (ReferenceEquals(mgr.GameClock, this)) mgr.GameClock = null;
        mgr.RemoveGrowthModifier(this);
    }
 
    public float TotalGameDays
    {
        get
        {
            if (timeManager == null) return 0f;
 
            float withinDay = (timeManager.CurrentHour * 60f + timeManager.CurrentMinute) / 1440f;
            return (timeManager.CurrentDay - 1) + withinDay;
        }
    }
 
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
 
    public float QualityBonus => 0f;   
 
    public bool AllowBestQuality => true;
 
    public bool CanPlantNow(CropSO crop)
    {
        if (!enforceSeason || crop == null || timeManager == null) return true;
 
        return (crop.plantableSeasons & ToCropSeason(timeManager.CurrentSeason)) != 0;
    }
 
    private static CropSeason ToCropSeason(TimeManager.SeasonPeriod season) => season switch
    {
        TimeManager.SeasonPeriod.Spring => CropSeason.Spring,
        TimeManager.SeasonPeriod.Summer => CropSeason.Summer,
        TimeManager.SeasonPeriod.Autumn => CropSeason.Autumn,
        _ => CropSeason.Winter,
    };
}