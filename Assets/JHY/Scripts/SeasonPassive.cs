using System.Collections;
using UnityEngine;

public class SeasonPassive : MonoBehaviour
{
    [SerializeField] private GameObject snowEffect;
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject flowerEffect;
    [SerializeField] private GameObject leavesEffect;
    [SerializeField] private AudioClip rain;

    public static SeasonPassive Instance;
    private bool isInsideShop;
    private bool shopOpen;
    private bool insideBuilding;
    private Coroutine weatherRoutine;
    private GameObject currentActiveEffect;
    private AudioSource currentAudioSource;
    private ParticleSystem[] activeParticles;
    private float[] originalRates;
    private ParticleSystemRenderer[] activeRenderers;
    private bool[] originalRendererVisibility;
    private bool isDraining;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDisable()
    {
        CancelWeather();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void HideActiveEffectForShop()
    {
        shopOpen = true;
        UpdateIndoorVisibility();
    }

    public void RestoreEffectAfterShop()
    {
        shopOpen = false;
        UpdateIndoorVisibility();
    }

    public void SetInsideBuilding(bool inside)
    {
        insideBuilding = inside;
        UpdateIndoorVisibility();
    }

    private void UpdateIndoorVisibility()
    {
        if (shopOpen || insideBuilding) HideWeather();
        else RestoreWeather();
    }

    private void HideWeather()
    {
        if (isInsideShop) return;
        isInsideShop = true;
        if (activeParticles != null)
            foreach (var particle in activeParticles) particle.Pause(false);
        if (activeRenderers != null)
            foreach (var renderer in activeRenderers) renderer.forceRenderingOff = true;
        if (currentAudioSource != null) currentAudioSource.Pause();
    }

    private void RestoreWeather()
    {
        if (!isInsideShop) return;
        isInsideShop = false;
        if (activeRenderers != null)
            for (int i = 0; i < activeRenderers.Length; i++)
                activeRenderers[i].forceRenderingOff = originalRendererVisibility[i];
        if (activeParticles != null)
            foreach (var particle in activeParticles)
            {
                particle.Play(false);
                if (isDraining) particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        if (currentAudioSource != null && !isDraining) currentAudioSource.UnPause();
    }

    public void ApplySeasonPassive(TimeManager.SeasonPeriod season)
    {
        CancelWeather();
        switch (season)
        {
            case TimeManager.SeasonPeriod.Spring: weatherRoutine = StartCoroutine(ApplyFlower()); break;
            case TimeManager.SeasonPeriod.Summer: weatherRoutine = StartCoroutine(ApplyRain()); break;
            case TimeManager.SeasonPeriod.Autumn: weatherRoutine = StartCoroutine(ApplyLeaves()); break;
            case TimeManager.SeasonPeriod.Winter: weatherRoutine = StartCoroutine(ApplySnow()); break;
        }
    }

    private IEnumerator ApplyRain() => TryWeather(rainEffect, rain);
    private IEnumerator ApplySnow() => TryWeather(snowEffect);
    private IEnumerator ApplyLeaves() => TryWeather(leavesEffect);
    private IEnumerator ApplyFlower() => TryWeather(flowerEffect);

    private IEnumerator TryWeather(GameObject effect, AudioClip clip = null)
    {
        if (Random.Range(0, 3) != 0) yield break;
        yield return new WaitForSeconds(Random.Range(2f, 60f));
        yield return PlayEffect(effect, clip);
    }

    private IEnumerator PlayEffect(GameObject effect, AudioClip clip = null)
    {
        if (effect == null) yield break;
        while (isInsideShop) yield return null;
        currentActiveEffect = effect;
        activeParticles = effect.GetComponentsInChildren<ParticleSystem>(true);
        activeRenderers = effect.GetComponentsInChildren<ParticleSystemRenderer>(true);
        originalRates = new float[activeParticles.Length];
        originalRendererVisibility = new bool[activeRenderers.Length];
        for (int i = 0; i < activeRenderers.Length; i++)
            originalRendererVisibility[i] = activeRenderers[i].forceRenderingOff;
        for (int i = 0; i < activeParticles.Length; i++)
            originalRates[i] = activeParticles[i].emission.rateOverTimeMultiplier;

        effect.SetActive(true);
        foreach (var particle in activeParticles)
        {
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(false);
        }
        if (clip != null)
        {
            currentAudioSource = GetComponent<AudioSource>();
            if (currentAudioSource == null) currentAudioSource = gameObject.AddComponent<AudioSource>();
            currentAudioSource.clip = clip;
            currentAudioSource.loop = false;
            currentAudioSource.volume = 1f;
            currentAudioSource.Play();
        }

        float elapsed = 0f;
        float targetDuration = Random.Range(120f, 181f);
        while (elapsed < targetDuration)
        {
            if (!isInsideShop) elapsed += Time.deltaTime;
            yield return null;
        }
        float fadeDuration = GetFadeDuration(effect);
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            while (isInsideShop) yield return null;
            elapsed += Time.deltaTime;
            float multiplier = Mathf.Clamp01(1f - elapsed / fadeDuration);
            for (int i = 0; i < activeParticles.Length; i++)
            {
                var emission = activeParticles[i].emission;
                emission.rateOverTimeMultiplier = originalRates[i] * multiplier;
            }
            if (currentAudioSource != null) currentAudioSource.volume = multiplier;
            yield return null;
        }

        while (isInsideShop) yield return null;
        isDraining = true;
        foreach (var particle in activeParticles)
            particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        if (currentAudioSource != null) currentAudioSource.Stop();
        while (HasLiveParticles()) yield return null;
        ResetEffect();
        weatherRoutine = null;
    }

    private bool HasLiveParticles()
    {
        foreach (var particle in activeParticles)
            if (particle.IsAlive(false)) return true;
        return false;
    }

    private void CancelWeather()
    {
        if (weatherRoutine != null) StopCoroutine(weatherRoutine);
        weatherRoutine = null;
        ResetEffect();
    }

    private void ResetEffect()
    {
        if (activeParticles != null)
            for (int i = 0; i < activeParticles.Length; i++)
            {
                if (activeParticles[i] == null) continue;
                activeParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = activeParticles[i].emission;
                emission.rateOverTimeMultiplier = originalRates[i];
            }
        if (activeRenderers != null)
            for (int i = 0; i < activeRenderers.Length; i++)
                if (activeRenderers[i] != null) activeRenderers[i].forceRenderingOff = originalRendererVisibility[i];
        if (currentActiveEffect != null) currentActiveEffect.SetActive(false);
        if (currentAudioSource != null) currentAudioSource.Stop();
        currentActiveEffect = null;
        currentAudioSource = null;
        activeParticles = null;
        activeRenderers = null;
        originalRates = null;
        originalRendererVisibility = null;
        isDraining = false;
    }

    private float GetFadeDuration(GameObject effect)
    {
        if (effect == flowerEffect || effect == leavesEffect) return 1f;
        return effect == rainEffect ? 2.5f : 2f;
    }

    [ContextMenu("Preview Weather/Rain")]
    private void PreviewRain() => Preview(rainEffect, rain);
    [ContextMenu("Preview Weather/Snow")]
    private void PreviewSnow() => Preview(snowEffect);
    [ContextMenu("Preview Weather/Flowers")]
    private void PreviewFlowers() => Preview(flowerEffect);
    [ContextMenu("Preview Weather/Leaves")]
    private void PreviewLeaves() => Preview(leavesEffect);
    private void Preview(GameObject effect, AudioClip clip = null)
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        CancelWeather();
        weatherRoutine = StartCoroutine(PlayEffect(effect, clip));
    }
}
