using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightManager : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;

    private Coroutine lightCoroutine;

    public void ChangeTimePeriod(TimeManager.TimePeriod newPeriod)
    {
        Color targetColor;
        float targetIntensity;

        switch (newPeriod)
        {
            case TimeManager.TimePeriod.Morning:
                targetColor = Color.white;
                targetIntensity = 1f;
                break;

            case TimeManager.TimePeriod.Afternoon:
                targetColor = new Color(183f/255f, 123f/255f, 50f/255f, 255f/255f);
                targetIntensity = 0.6f;
                break;

            case TimeManager.TimePeriod.Night:
                targetColor = new Color(39f/255f, 39f/255f, 39f/255f, 255f/255f);
                targetIntensity = 0.2f;
                break;

            default:
                return;
        }

        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);

        lightCoroutine = StartCoroutine(ChangeLight(targetColor, targetIntensity, 3f));
    }

    private IEnumerator ChangeLight(Color targetColor, float targetIntensity, float duration)
    {
        Color startColor = globalLight.color;
        float startIntensity = globalLight.intensity;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            globalLight.color = Color.Lerp(startColor, targetColor, t);
            globalLight.intensity = Mathf.Lerp(
                startIntensity,
                targetIntensity,
                t
            );

            yield return null;
        }

        globalLight.color = targetColor;
        globalLight.intensity = targetIntensity;
    }
}
