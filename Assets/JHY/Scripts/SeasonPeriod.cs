using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPeriod : MonoBehaviour
{
    [SerializeField]private GameObject spring;
    [SerializeField] private GameObject summer;
    [SerializeField] private GameObject autumn;
    [SerializeField] private GameObject winter;

    [SerializeField] private GameObject DarkPannel;
    private bool isTransitioning = false;

    public void ChangeSeasonPeriod(TimeManager.SeasonPeriod newSeason)//타일맵 swap + 각 계절별 제철 과일들로 상점 갱신
    {
        if (isTransitioning)
            return;

        StartCoroutine(FadeInOut(newSeason));
    }
    
    private IEnumerator FadeInOut(TimeManager.SeasonPeriod newSeason)
    {
        Image panelImage = DarkPannel.GetComponent<Image>();

        DarkPannel.SetActive(true);

        // 처음에는 투명
        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            0f
        );

        // 1. 서서히 어두워짐
        yield return panelImage.DOFade(1f, 0.8f).WaitForCompletion();

        // 2. 완전히 어두워진 순간 계절 변경
        SetSeason(newSeason);

        // 3. 바로 서서히 밝아짐
        yield return panelImage.DOFade(0f, 1f).WaitForCompletion();

        DarkPannel.SetActive(false);
    }
    private void SetSeason(TimeManager.SeasonPeriod newSeason)
    {
        spring.SetActive(false);
        summer.SetActive(false);
        autumn.SetActive(false);
        winter.SetActive(false);

        switch (newSeason)
        {
            case TimeManager.SeasonPeriod.Spring:
                spring.SetActive(true);
                Debug.Log("Spring");
                break;

            case TimeManager.SeasonPeriod.Summer:
                summer.SetActive(true);
                Debug.Log("Summer");
                break;

            case TimeManager.SeasonPeriod.Autumn:
                autumn.SetActive(true);
                Debug.Log("Autumn");
                break;

            case TimeManager.SeasonPeriod.Winter:
                winter.SetActive(true);
                Debug.Log("Winter");
                break;
        }
    }
}
