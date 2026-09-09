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
 
        if (timeManager == null)
            Debug.LogWarning("[계절연동] 씬에서 TimeManager 를 찾지 못했습니다. 작물이 자체 시계로 돕니다.", this);
    }
 
    private void OnEnable()
    {
        CropManager mgr = CropManager.Instance;
        if (mgr == null) return;
 
        mgr.GameClock = this;
        mgr.GrowthModifier = this;
    }
 
    private void OnDisable()
    {
        CropManager mgr = CropManager.Instance;
        if (mgr == null) return;
 
        if (ReferenceEquals(mgr.GameClock, this)) mgr.GameClock = null;
        if (ReferenceEquals(mgr.GrowthModifier, this)) mgr.GrowthModifier = null;
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