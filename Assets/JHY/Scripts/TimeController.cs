using UnityEngine;

public class TimeController : MonoBehaviour
{
    private TimePeriod timePeriod;
    private SeasonPeriod seasonPeriod;
    private SeasonPassive seasonPassive;

    [SerializeField] private LightManager lightManager;
    private void Awake()
    {
        timePeriod = GetComponent<TimePeriod>();
        seasonPeriod = GetComponent<SeasonPeriod>();
        seasonPassive = GetComponent<SeasonPassive>();
    }
    private void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimePeriodChange += HandleTimePeriod;
            TimeManager.Instance.OnTimePeriodChange += HandleLight;
            TimeManager.Instance.OnSeasonChange += HandleSeasonPeriod;
            TimeManager.Instance.OnDayChange += HandleSeasonPassive;
            HandleTimePeriod(TimeManager.Instance.CurrentPeriod);
            HandleSeasonPeriod(TimeManager.Instance.CurrentSeason);
            if (lightManager != null) HandleLight(TimeManager.Instance.CurrentPeriod);
        }
    }
    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimePeriodChange -= HandleTimePeriod;
            TimeManager.Instance.OnTimePeriodChange -= HandleLight;
            TimeManager.Instance.OnSeasonChange -= HandleSeasonPeriod;
            TimeManager.Instance.OnDayChange -= HandleSeasonPassive;
        }
    }
    private void HandleLight(TimeManager.TimePeriod currentPeriod)
    {
        lightManager.LightChangeTimePeriod(currentPeriod);
    }
    private void HandleSeasonPassive(TimeManager.SeasonPeriod currentSeason)
    {
        seasonPassive.ApplySeasonPassive(currentSeason);
    }
    private void HandleTimePeriod(TimeManager.TimePeriod currentPeriod)
    {
        timePeriod.ChangeTimePeriod(currentPeriod);
    }
    private void HandleSeasonPeriod(TimeManager.SeasonPeriod currentSeason)
    {
        seasonPeriod.ChangeSeasonPeriod(currentSeason);
    }
}
