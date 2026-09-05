using System;
using System.Collections;
using UnityEngine;

public class SeasonPeriod : MonoBehaviour
{
    [SerializeField]private GameObject spring;
    [SerializeField] private GameObject summer;
    [SerializeField] private GameObject autumn;
    [SerializeField] private GameObject winter;

    [SerializeField] private GameObject DarkPannel;

    public void ChangeSeasonPeriod(TimeManager.SeasonPeriod newSeason)//타일맵 swap + 각 계절별 제철 과일들로 상점 갱신
    {
        switch (newSeason)
        {
            case TimeManager.SeasonPeriod.Spring:
                StartCoroutine(FadeInOut());
                Debug.Log("Spring");
                spring.SetActive(true);
               summer.SetActive(false);
                autumn.SetActive(false);
                winter.SetActive(false);
                //상점 갱신
                break;
            case TimeManager.SeasonPeriod.Summer:
                StartCoroutine(FadeInOut());
                Debug.Log("Summer");
                spring.SetActive(false);
                summer.SetActive(true);
                autumn.SetActive(false);
                winter.SetActive(false);
                //상점갱신
                break;
            case TimeManager.SeasonPeriod.Autumn:
                StartCoroutine(FadeInOut());
                Debug.Log("Autumn");
                spring.SetActive(false);
               summer.SetActive(false);
                autumn.SetActive(true);
                winter.SetActive(false);
                //상점갱신
                break;
            case TimeManager.SeasonPeriod.Winter:
                StartCoroutine(FadeInOut());
                Debug.Log("Winter");
                spring.SetActive(false);
                summer.SetActive(false);
               autumn.SetActive(false);
                winter.SetActive(true);
                //상점갱신
                break;
        }
    }
    private IEnumerator FadeInOut() 
    {
        DarkPannel.SetActive(true);

        yield return new WaitForSeconds(1f);

        DarkPannel.SetActive(false);
    }
}
