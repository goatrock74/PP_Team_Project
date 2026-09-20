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
        Image panelImage = DarkPannel.GetComponent<Image>();

        // 1. 패널 켜고 즉시 완전히 어둡게(알파 1) 설정
        DarkPannel.SetActive(true);
        panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 1f);

        // 2. 검은 화면 유지 대기 시간 (원하는 만큼 조절 가능)
        yield return new WaitForSeconds(0.5f);

        // 3. 0.5초 동안 부드럽게 다시 밝아짐 (Fade Out)
        yield return panelImage.DOFade(0f, 0.5f).WaitForCompletion();

        // 4. 패널 끄기
        DarkPannel.SetActive(false);
    }
}
