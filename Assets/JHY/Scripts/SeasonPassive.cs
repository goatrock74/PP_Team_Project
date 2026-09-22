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
        if (UnityEngine.Random.Range(0, 3) != 0)
            yield break;

        float start = UnityEngine.Random.Range(1.5f, 2f);

        yield return new WaitForSeconds(start);

        Debug.Log("Rain");

        yield return StartCoroutine(PlayEffect(rainEffect));
    }


    private IEnumerator ApplySnow()
    {
        if (UnityEngine.Random.Range(0, 3) != 0)
            yield break;

        float start = UnityEngine.Random.Range(1.5f, 2f);

        yield return new WaitForSeconds(start);

        Debug.Log("Snow");

        yield return StartCoroutine(PlayEffect(snowEffect));
    }


    private IEnumerator ApplyLeaves()
    {
        if (UnityEngine.Random.Range(0, 3) != 0)
            yield break;

        float start = UnityEngine.Random.Range(1.5f, 2f);

        yield return new WaitForSeconds(start);

        Debug.Log("Leaves");

        yield return StartCoroutine(PlayEffect(leavesEffect));
    }


    private IEnumerator ApplyFlower()
    {
        if (UnityEngine.Random.Range(0, 3) != 0)
            yield break;

        float start = UnityEngine.Random.Range(1.5f, 2f);

        yield return new WaitForSeconds(start);

        Debug.Log("Flower");

        yield return StartCoroutine(PlayEffect(flowerEffect));
    }


    private IEnumerator PlayEffect(GameObject effect)
    {
        if (effect == null)
            yield break;

        ParticleSystem[] particles =
            effect.GetComponentsInChildren<ParticleSystem>(true);

        effect.SetActive(true);

        float[] originalRates = new float[particles.Length];

        // 원래 Rate 저장
        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;

            originalRates[i] = emission.rateOverTime.constant;
        }


        // 8초 동안 정상적으로 내림
        yield return new WaitForSeconds(8f);


        // Rate를 서서히 감소
        float fadeDuration = GetFadeDuration(effect);

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;

            // 1 → 0
            float rateMultiplier = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < particles.Length; i++)
            {
                var emission = particles[i].emission;

                emission.rateOverTime =
                    originalRates[i] * rateMultiplier;
            }

            yield return null;
        }


        // Rate 완전히 0
        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;

            emission.rateOverTime = 0f;
        }


        // ⭐ 이미 생성된 파티클이 전부 사라질 때까지 기다림
        bool isAlive = true;

        while (isAlive)
        {
            isAlive = false;

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].IsAlive(true))
                {
                    isAlive = true;
                    break;
                }
            }

            yield return null;
        }


        // ⭐ 모든 파티클이 사라진 후에만 비활성화
        effect.SetActive(false);


        // 다음 사용을 위해 원래 Rate 복구
        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;

            emission.rateOverTime = originalRates[i];
        }
    }


    private float GetFadeDuration(GameObject effect)
    {
        if (effect == flowerEffect || effect == leavesEffect)
            return 1f;

        if (effect == snowEffect)
            return 2f;

        if (effect == rainEffect)
            return 2.5f;

        return 2f;
    }
}
