using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class TimePeriod : MonoBehaviour
{
    [SerializeField] private CanvasGroup sun;
    [SerializeField] private CanvasGroup afternoon;
    [SerializeField] private CanvasGroup night;
    [SerializeField] private GameObject DarkPannel;

    [SerializeField] private GameObject nightNpc;
    [SerializeField] private int percent = 3;
    public bool IsFading { get; private set; } = false;
    private Coroutine npcSpawnCoroutine;
    [SerializeField] private AudioClip morning;
    [SerializeField] private AudioClip Night;
    public void TriggerNextDay()
    {
        if (IsFading) return;

        StartCoroutine(StartNextDay());
    }
    public void ChangeTimePeriod(TimeManager.TimePeriod newPeriod) 
    {
        if (npcSpawnCoroutine != null)
        {
            StopCoroutine(npcSpawnCoroutine);
            npcSpawnCoroutine = null;
        }
        switch (newPeriod)
        {
            case TimeManager.TimePeriod.Morning:
                Debug.Log("sun");
                if (!OceanTeleporter.IsInOcean && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBGM(morning);
                }
                sun.alpha = 1;
                afternoon.alpha = 0;
                night.alpha = 0;
                if (nightNpc != null) nightNpc.SetActive(false);
                break;
            case TimeManager.TimePeriod.Afternoon:
                Debug.Log("afternoon");
                sun.alpha = 0;
                afternoon.alpha = 1;
                night.alpha = 0;

                if (nightNpc != null) nightNpc.SetActive(false);
                break;
            case TimeManager.TimePeriod.Night:
                Debug.Log("night");
                if (!OceanTeleporter.IsInOcean && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBGM(Night);
                }

                sun.alpha = 0;
                afternoon.alpha = 0;
                night.alpha = 1;
                if (nightNpc != null)
                {
                    nightNpc.SetActive(false);
                    npcSpawnCoroutine = StartCoroutine(NPCSpawn());
                }
                break;
        }
    }
    private IEnumerator NPCSpawn()
    {
        bool shouldSpawn = Random.Range(0, percent) == 0;
        yield return new WaitForSeconds(2f);

        nightNpc.SetActive(shouldSpawn);

        // 진짜로 켜졌을 때만 로그 출력
        if (shouldSpawn)
        {
            Debug.Log("NPC등장");
        }
    }
    private IEnumerator StartNextDay()
    {
        IsFading = true;

        Image panelImage = DarkPannel.GetComponent<Image>();
        panelImage.DOKill();
        panelImage.color = new Color(
            panelImage.color.r,
            panelImage.color.g,
            panelImage.color.b,
            0f
        );
        DarkPannel.SetActive(true);

        yield return panelImage.DOFade(1f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        TimeManager.Instance.SkipToNextDay();

        yield return panelImage.DOFade(0f, 1.5f)
            .SetEase(Ease.Linear)
            .WaitForCompletion();

        DarkPannel.SetActive(false);

        IsFading = false;
    }
}
