using System;
using System.Collections;
using UnityEngine;
                        
public class SeasonPassive : MonoBehaviour
{
    [SerializeField] private GameObject snowEffect;
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject flowerEffect;
    [SerializeField] private GameObject leavesEffect;

    [SerializeField] private AudioClip rain;

    public void ApplySeasonPassive(TimeManager.SeasonPeriod season)
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

        yield return StartCoroutine(PlayEffect(rainEffect, rain));
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

    private IEnumerator PlayEffect(GameObject effect, AudioClip clip = null)
    {
        if (effect == null)
            yield break;

        if (effect.activeSelf) yield break;

        AudioSource audioSource = null;
        if (clip != null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (SoundManager.Instance != null)
            {
            }

            audioSource.clip = clip;
            audioSource.loop = false; // 음원이 길므로 루프 안 함!
            audioSource.volume = 1f;
            audioSource.Play();
        }

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        effect.SetActive(true);

        float[] originalRates = new float[particles.Length];

        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;
            originalRates[i] = emission.rateOverTime.constant;
        }

        yield return new WaitForSeconds(8f);

        float fadeDuration = GetFadeDuration(effect);
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            float rateMultiplier = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < particles.Length; i++)
            {
                var emission = particles[i].emission;
                emission.rateOverTime = originalRates[i] * rateMultiplier;
            }

            if (audioSource != null)
            {
                audioSource.volume = rateMultiplier;
            }

            yield return null;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            var emission = particles[i].emission;
            emission.rateOverTime = 0f;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

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

        effect.SetActive(false);

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
