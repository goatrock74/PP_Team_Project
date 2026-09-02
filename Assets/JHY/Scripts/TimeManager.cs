using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public enum TimePeriod
    {
        Morning,
        Afternoon,
        Night
    }
    public enum SeasonPeriod
    {
        Spring,
        Summer,
        Authum,
        Winter
    }
    [Header("Time Settings")]
    [SerializeField] private float dayDuration = 720f;

    #region 변수
    private int currentDay = 1;
    private TimePeriod currentPeriod = TimePeriod.Morning;
    private SeasonPeriod currentSeason = SeasonPeriod.Spring;
    private float currentTimeInSeconds = 0;

    public int CurrentDay => currentDay;
    public TimePeriod CurrentPeriod => currentPeriod;
    public SeasonPeriod CurrentSeason => currentSeason;
    public int CurrentHour => Mathf.FloorToInt((currentTimeInSeconds / dayDuration) * 24f);
    public int CurrentMinute => Mathf.FloorToInt(((currentTimeInSeconds / dayDuration) * 24f % 1f) * 60f);
    #endregion

    //public event Action<int> OnDayChange;
    public event Action<SeasonPeriod> OnSeasonChange;
    public event Action<TimePeriod> OnTimePeriodChange;
    private void Start()
    {
        Debug.Log("Time: "+ currentPeriod);
        Debug.Log("Day: " + currentDay);
        Debug.Log("Season: " + currentSeason);
    }
    private void Update()
    {
        currentTimeInSeconds += Time.deltaTime;

        if (currentTimeInSeconds >= dayDuration)
        {
            currentTimeInSeconds -= dayDuration;
            currentDay++;
            Debug.Log("Day: " + currentDay);
            UpdateSeasonPeriod();
        }
        UpdateTimePeriod();
    }
    private void UpdateSeasonPeriod()
    {
        int seasonIndex = ((currentDay - 1) / 3) % 4;
        SeasonPeriod newSeason = (SeasonPeriod)seasonIndex;

        if (newSeason != currentSeason)
        {
            currentSeason = newSeason;
            OnSeasonChange?.Invoke(currentSeason);
        }
    }
    private void UpdateTimePeriod()
    {
        int hour = CurrentHour;
        TimePeriod newPeriod;

        if (hour >= 0 && hour < 14)
        {
            newPeriod = TimePeriod.Morning;
        }
        else if (hour >= 14 && hour < 18)
        {
            newPeriod = TimePeriod.Afternoon;
        }
        else
        {
            newPeriod = TimePeriod.Night;
        }

        if (newPeriod != currentPeriod)
        {
            currentPeriod = newPeriod;
            OnTimePeriodChange?.Invoke(currentPeriod);
        }
    }
}
