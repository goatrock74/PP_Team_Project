using TMPro;
using UnityEngine;

public class TimeUIManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI dayText;

    private void Start()
    {
        UpdateDayText();

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChange += OnDayChangedHandler;
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChange -= OnDayChangedHandler;
        }
    }

    private void OnDayChangedHandler(TimeManager.SeasonPeriod season)
    {
        UpdateDayText();
    }

    private void UpdateDayText()
    {
        if (TimeManager.Instance != null && dayText != null)
        {
            dayText.text = "Day: " + TimeManager.Instance.CurrentDay.ToString();
        }
    }
}
