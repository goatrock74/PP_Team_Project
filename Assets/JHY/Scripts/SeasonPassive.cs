using System;
using System.Collections;
using UnityEngine;
                        
public class SeasonPassive : MonoBehaviour
{
    [SerializeField] private GameObject snowEffect;
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject flowerEffect;
    [SerializeField] private GameObject leavesEffect;

    
    public void ApplySeasonPassive(TimeManager.SeasonPeriod season) //각 특성
    {
        switch (season)
        {
            case TimeManager.SeasonPeriod.Spring:
                StartCoroutine(ApplyFlower());
                break;
            case TimeManager.SeasonPeriod.Summer:
                StartCoroutine(ApplyRain());
                break;
            case TimeManager.SeasonPeriod.Autumn:
                StartCoroutine(ApplyLeaves());
                break;
            case TimeManager.SeasonPeriod.Winter:
               StartCoroutine(ApplySnow());
                break;
        }
    }

    private IEnumerator ApplyRain()
    {
        if(UnityEngine.Random.Range(0,3) == 0)
        {
            float start = UnityEngine.Random.Range(1.5f, 2f);
            int end = UnityEngine.Random.Range(8, 12);
            yield return new WaitForSeconds(start);
            Debug.Log("Rain");
            rainEffect.SetActive(true);
            yield return new WaitForSeconds(end);
            rainEffect.SetActive(false);
        }
    }

    private IEnumerator ApplySnow()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            float start = UnityEngine.Random.Range(1.5f, 2f);
            int end = UnityEngine.Random.Range(8, 12);
            yield return new WaitForSeconds(start);
            Debug.Log("Snow");
            snowEffect.SetActive(true);
            yield return new WaitForSeconds(end);
            snowEffect.SetActive(false);
        }
    }
    private IEnumerator ApplyLeaves()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            float start = UnityEngine.Random.Range(1.5f, 2f);
            int end = UnityEngine.Random.Range(8, 12);
            yield return new WaitForSeconds(start);
            Debug.Log("Leaves");
            leavesEffect.SetActive(true);
            yield return new WaitForSeconds(end);
            leavesEffect.SetActive(false);
        }
    }
    private IEnumerator ApplyFlower()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            float start = UnityEngine.Random.Range(1.5f, 2f);
            int end = UnityEngine.Random.Range(8, 12);
            yield return new WaitForSeconds(start);
            Debug.Log("Flower");
            flowerEffect.SetActive(true);
            yield return new WaitForSeconds(end);
            flowerEffect.SetActive(false);
        }
    }
}
