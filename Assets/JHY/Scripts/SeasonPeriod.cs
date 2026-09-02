using System.Collections;
using UnityEngine;

public class SeasonPeriod : MonoBehaviour
{
    [SerializeField]private GameObject spring;
    [SerializeField] private GameObject summer;
    [SerializeField] private GameObject autumn;
    [SerializeField] private GameObject winter;

    [SerializeField] private GameObject DarkPannel;
    public void ChangeSeasonPeriod(TimeManager.SeasonPeriod newSeason)
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
                break;
            case TimeManager.SeasonPeriod.Summer:
                StartCoroutine(FadeInOut());
                Debug.Log("Summer");
                spring.SetActive(false);
                summer.SetActive(true);
                autumn.SetActive(false);
                winter.SetActive(false);
                break;
            case TimeManager.SeasonPeriod.Authum:
                StartCoroutine(FadeInOut());
                Debug.Log("Authum");
                spring.SetActive(false);
                summer.SetActive(false);
                autumn.SetActive(true);
                winter.SetActive(false);
                break;
            case TimeManager.SeasonPeriod.Winter:
                StartCoroutine(FadeInOut());
                Debug.Log("Winter");
                spring.SetActive(false);
                summer.SetActive(false);
                autumn.SetActive(false);
                winter.SetActive(true);
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
