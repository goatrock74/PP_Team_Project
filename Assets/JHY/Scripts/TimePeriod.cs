using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TimePeriod : MonoBehaviour
{
    [SerializeField] private CanvasGroup sun;
    [SerializeField] private CanvasGroup afternoon;
    [SerializeField] private CanvasGroup night;
    [SerializeField] private GameObject DarkPannel;

    private bool isFading = false;

    public void TriggerFade()
    {
        if (isFading) return;

        StartCoroutine(StartFade());
    }
    public void ChangeTimePeriod(TimeManager.TimePeriod newPeriod) 
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
    private IEnumerator StartFade()
    {
        isFading = true;

        Image panelImage = DarkPannel.GetComponent<Image>();

        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            0f
        );
        DarkPannel.SetActive(true);

        yield return panelImage.DOFade(1f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        TimeManager.Instance.SkipToNextDay();

        yield return panelImage.DOFade(0f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        DarkPannel.SetActive(false);

        isFading = false;
    }
}
