using System;
using System.Collections;
using UnityEngine;
                        
public class SeasonPassive : MonoBehaviour
{
    [SerializeField] private GameObject snowEffect;
    public void ApplySeasonPassive(TimeManager.SeasonPeriod season) //각 특성
    {
        switch (season)
        {
            case TimeManager.SeasonPeriod.Spring:
                ApplyRain();
                break;
            case TimeManager.SeasonPeriod.Summer:
                ApplyHot();
                break;
            case TimeManager.SeasonPeriod.Autumn:
                ApplyLeaves();
                break;
            case TimeManager.SeasonPeriod.Winter:
               StartCoroutine(ApplySnow());
                break;
        }
    }

    private void ApplyRain()
    {
        if(UnityEngine.Random.Range(0,3) == 0)
        {
            Debug.Log("Rain");
        }
    }

    private IEnumerator ApplySnow()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            int start = UnityEngine.Random.Range(0, 1);
            int end = UnityEngine.Random.Range(3,5);
            yield return new WaitForSeconds(start);
            Debug.Log("Snow");
            snowEffect.SetActive(true);
            yield return new WaitForSeconds(end);
            snowEffect.SetActive(false);
        }
    }
    private void ApplyLeaves()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            Debug.Log("Leaves");
        }
    }
    private void ApplyHot()
    {
        if (UnityEngine.Random.Range(0, 3) == 0)
        {
            Debug.Log("Hot");
        }
    }
}
