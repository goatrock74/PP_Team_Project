using UnityEngine;

public class TimePeriod : MonoBehaviour
{
    [SerializeField] private CanvasGroup sun;
    [SerializeField] private CanvasGroup afternoon;
    [SerializeField] private CanvasGroup night;
   
    public void ChangeTimePeriod(TimeManager.TimePeriod newPeriod) //global light 2d로 밝기 조절 + 시간대별 상점 갱신
    { //시간대 별로도 상점 갱신해야되고 계절 바뀔떄도 상점 갱신해야되니까 상점 매니저를 따로 만들어서 쓰기.
        switch (newPeriod)
        {
            case TimeManager.TimePeriod.Morning:
                Debug.Log("sun");
                sun.alpha = 1;
                afternoon.alpha = 0;
                night.alpha = 0;
                //상점 갱신
                break;
            case TimeManager.TimePeriod.Afternoon:
                Debug.Log("afternoon");
                sun.alpha = 0;
                afternoon.alpha = 1;
                night.alpha = 0;
                //상점 갱신
                break;
            case TimeManager.TimePeriod.Night:
                Debug.Log("night");
                sun.alpha = 0;
                afternoon.alpha = 0;
                night.alpha = 1;
                //상점 갱신
                break;
        }
    }
}
