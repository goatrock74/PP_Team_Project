using UnityEngine;

public class DayPeriod : MonoBehaviour
{
    [SerializeField] private CanvasGroup sun;
    [SerializeField] private CanvasGroup afternoon;
    [SerializeField] private CanvasGroup night;
    public void CPeriod(TimeManager.TimePeriod currentPeriod)
    {
        ChangePeriod(currentPeriod);
    }
    private void ChangePeriod(TimeManager.TimePeriod newPeriod) //페이드인 페이드아웃 서서히 사라지는걸로
    {
        switch (newPeriod)
        {
            case TimeManager.TimePeriod.Morning:
                Debug.Log("sun");
                sun.alpha = 1;
                afternoon.alpha = 0;
                night.alpha = 0;
                break;
            case TimeManager.TimePeriod.Afternoon:
                Debug.Log("afternoon");
                sun.alpha = 0;
                afternoon.alpha = 1;
                night.alpha = 0;
                break;
            case TimeManager.TimePeriod.Night:
                Debug.Log("night");
                sun.alpha = 0;
                afternoon.alpha = 0;
                night.alpha = 1;
                break;
        }
    }
}
