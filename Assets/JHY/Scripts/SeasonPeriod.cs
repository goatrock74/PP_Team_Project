using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPeriod : MonoBehaviour
{
    [SerializeField] private GameObject spring;
    [SerializeField] private GameObject summer;
    [SerializeField] private GameObject autumn;
    [SerializeField] private GameObject winter;

    [SerializeField] private GameObject DarkPannel;

    private bool isTransitioning = false;

    private void Start()
    {
        // 현재 계절 먼저 적용
        SetSeason(TimeManager.Instance.CurrentSeason);

        // 게임 시작 시 검은 화면 → 천천히 밝아짐
        StartCoroutine(StartFade());
    }

    private IEnumerator StartFade()
    {
        Image panelImage = DarkPannel.GetComponent<Image>();

        DarkPannel.SetActive(true);

        // 처음에는 완전히 검은색
        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            1f
        );

        // 1.5초 동안 검은색 → 투명
        yield return panelImage.DOFade(0f, 3f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        DarkPannel.SetActive(false);
    }

    public void ChangeSeasonPeriod(TimeManager.SeasonPeriod newSeason)
    {
        if (isTransitioning)
            return;

        StartCoroutine(FadeInOut(newSeason));
    }

    private IEnumerator FadeInOut(TimeManager.SeasonPeriod newSeason)
    {
        Image panelImage = DarkPannel.GetComponent<Image>();

        isTransitioning = true;
        DarkPannel.SetActive(true);

        // 현재 화면에서 시작
        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            0f
        );

        // 1. 화면이 어두워짐
        yield return panelImage.DOFade(1f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        // 2. 완전히 어두워진 순간 계절 변경
        SetSeason(newSeason);

        // 3. 다시 밝아짐
        yield return panelImage.DOFade(0f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        DarkPannel.SetActive(false);
        isTransitioning = false;
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
