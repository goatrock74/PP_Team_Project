using UnityEngine;

public class DayPeriodController : MonoBehaviour
{
    private TimeManager timeManager;
    private DayPeriod day;

    private void Awake()
    {
        timeManager = GetComponent<TimeManager>();
        day = GetComponent<DayPeriod>();
    }
    private void OnEnable()
    {
        timeManager.OnTimePeriodChange += HandlePeriod;
    }
    private void OnDisable()
    {
        timeManager.OnTimePeriodChange -= HandlePeriod;
    }
    private void HandlePeriod(TimeManager.TimePeriod currentPeriod)
    {
        day.CPeriod(currentPeriod);
    }
}
