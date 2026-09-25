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

    public bool IsTransitioning { get; private set; } = false;
    private TimePeriod timePeriod;
    private void Awake()
    {
        timePeriod = GetComponent<TimePeriod>();
    }
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
        panelImage.DOKill(); // 충돌 방지

        DarkPannel.SetActive(true);
        panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 1f);

        yield return new WaitForSeconds(0.8f);

        yield return panelImage.DOFade(0f, 2f).SetEase(Ease.InCubic).WaitForCompletion();

        DarkPannel.SetActive(false);
    }

    public void ChangeSeasonPeriod(TimeManager.SeasonPeriod newSeason)
    {
        if (IsTransitioning)
            return;
        if (timePeriod != null && timePeriod.IsFading)
        {
            SetSeason(newSeason);
            return;
        }
        StartCoroutine(FadeInOut(newSeason));
    }

    private IEnumerator FadeInOut(TimeManager.SeasonPeriod newSeason)
    {
        Image panelImage = DarkPannel.GetComponent<Image>();

        IsTransitioning = true;
        panelImage.DOKill();
        DarkPannel.SetActive(true);

        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            0f
        );

        yield return panelImage.DOFade(1f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        SetSeason(newSeason);

        yield return panelImage.DOFade(0f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        DarkPannel.SetActive(false);
        IsTransitioning = false;
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
