using UnityEngine;

public class TimeController : MonoBehaviour
{
    private TimeManager timeManager;
    private TimePeriod timePeriod;
    private SeasonPeriod seasonPeriod;
    private SeasonPassive seasonPassive;

    
    [SerializeField] private LightManager lightManager;
    private void Awake()
    {
        timeManager = GetComponent<TimeManager>();
        timePeriod = GetComponent<TimePeriod>();
        seasonPeriod = GetComponent<SeasonPeriod>();
        seasonPassive = GetComponent<SeasonPassive>();
    }
    private void OnEnable()
    {
        timeManager.OnTimePeriodChange += HandleTimePeriod;
        timeManager.OnTimePeriodChange +=HandleLight;
        timeManager.OnSeasonChange += HandleSeasonPeriod;
        timeManager.OnDayChange += HandleSeasonPassive;
    }
    private void OnDisable()
    {
        timeManager.OnTimePeriodChange -= HandleTimePeriod;
        timeManager.OnTimePeriodChange -= HandleLight;
        timeManager.OnSeasonChange -= HandleSeasonPeriod;
        timeManager.OnDayChange -= HandleSeasonPassive;
    }
    private void HandleLight(TimeManager.TimePeriod currentPeriod)
    {
        lightManager.ChangeTimePeriod(currentPeriod);
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
