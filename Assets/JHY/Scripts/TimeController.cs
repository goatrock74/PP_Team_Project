using UnityEngine;

public class TimeController : MonoBehaviour
{
    private TimeManager timeManager;
    private TimePeriod timePeriod;
    private SeasonPeriod seasonPeriod;
    private void Awake()
    {
        timeManager = GetComponent<TimeManager>();
        timePeriod = GetComponent<TimePeriod>();
        seasonPeriod = GetComponent<SeasonPeriod>();
    }
    private void OnEnable()
    {
        timeManager.OnTimePeriodChange += HandleTimePeriod;
        timeManager.OnSeasonChange += HandleSeasonPeriod;
    }
    private void OnDisable()
    {
        timeManager.OnTimePeriodChange -= HandleTimePeriod;
        timeManager.OnSeasonChange -= HandleSeasonPeriod;
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
