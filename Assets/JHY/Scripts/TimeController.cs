using UnityEngine;
using UnityEngine.InputSystem;

public class TimeController : MonoBehaviour
{
    private TimePeriod timePeriod;
    private SeasonPeriod seasonPeriod;
    private SeasonPassive seasonPassive;
    private TimeManager subscribedManager;

    [SerializeField] private LightManager lightManager;
    private void Awake()
    {
        timePeriod = GetComponent<TimePeriod>();
        seasonPeriod = GetComponent<SeasonPeriod>();
        seasonPassive = GetComponent<SeasonPassive>();
    }
    private void Update()
    {
        if (subscribedManager != TimeManager.Instance) Subscribe();
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (timePeriod.IsFading || seasonPeriod.IsTransitioning)
                return;

            timePeriod.TriggerNextDay();
        }
    }
    private void Start()
    {
        Subscribe();
        if (TimeManager.Instance != null)
        {
            HandleTimePeriod(TimeManager.Instance.CurrentPeriod);
            if (lightManager != null) HandleLight(TimeManager.Instance.CurrentPeriod);

            HandleSeasonPeriod(TimeManager.Instance.CurrentSeason);
            HandleSeasonPassive(TimeManager.Instance.CurrentSeason);
        }
    }
    private void OnEnable()
    {
        Subscribe();
    }
    private void OnDisable()
    {
        Unsubscribe();
    }
    private void Subscribe()
    {
        if (subscribedManager == TimeManager.Instance) return;
        Unsubscribe();
        subscribedManager = TimeManager.Instance;
        if (subscribedManager == null) return;
        subscribedManager.OnTimePeriodChange += HandleTimePeriod;
        subscribedManager.OnTimePeriodChange += HandleLight;
        subscribedManager.OnSeasonChange += HandleSeasonPeriod;
        subscribedManager.OnDayChange += HandleSeasonPassive;
    }
    private void Unsubscribe()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnTimePeriodChange -= HandleTimePeriod;
            subscribedManager.OnTimePeriodChange -= HandleLight;
            subscribedManager.OnSeasonChange -= HandleSeasonPeriod;
            subscribedManager.OnDayChange -= HandleSeasonPassive;
        }
        subscribedManager = null;
    }
    private void HandleLight(TimeManager.TimePeriod currentPeriod)
    {
        if (lightManager != null) lightManager.LightChangeTimePeriod(currentPeriod);
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
