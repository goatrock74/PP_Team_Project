using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OceanTeleporter : MonoBehaviour
{
    [Header("이동 대상")]
    [SerializeField] private Transform playerTransform;

    [Header("목적지 설정")]
    [SerializeField] private Transform oceanDestination;
    [SerializeField] private Transform mainMapDestination;

    [Header("페이드 연출 UI")]
    [SerializeField] private GameObject darkPanel;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("BGM 설정")]
    [SerializeField] private AudioClip oceanBGM;
    [SerializeField] private AudioClip mainMapDayBGM;
    [SerializeField] private AudioClip mainMapNightBGM;

    [Header("갈매기 소리 설정")]
    [SerializeField] private AudioClip seagullSFX;
    [SerializeField] private float minSeagullInterval = 5f;
    [SerializeField] private float maxSeagullInterval = 12f;

    private bool isTeleporting = false;
    private Coroutine seagullCoroutine;
    public static bool IsInOcean { get; private set; } = false;

    public static OceanTeleporter Instance;
    private bool isSeagullPaused = false; 

    private void Awake()
    {
        if (Instance == null) { Instance = this; IsInOcean = false; }
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.ResetOutdoorSceneAudio();
        ResumeOutdoorBGM();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        IsInOcean = false;
    }
    public void PauseSeagullsForShop()
    {
        isSeagullPaused = true;
        if (seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopSFX(); 
        }
    }

    public void ResumeSeagullsForShop()
    {
        if (IsInOcean && isSeagullPaused)
        {
            isSeagullPaused = false;
            if (seagullCoroutine == null && seagullSFX != null)
            {
                seagullCoroutine = StartCoroutine(SeagullRoutine());
            }
        }
    }
    public void TeleportToOcean()
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(true));
    }
    public void TeleportToMainMap()
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(false));
    }
    private IEnumerator TeleportRoutine(bool isGoingToOcean)
    {
        isTeleporting = true;

        isSeagullPaused = false; 

        if (!isGoingToOcean && seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
        }

        Transform targetDestination = isGoingToOcean ? oceanDestination : mainMapDestination;
        if (targetDestination == null || playerTransform == null || darkPanel == null)
        {
            isTeleporting = false;
            yield break;
        }
        Image panelImage = darkPanel.GetComponent<Image>();
        if (panelImage == null)
        {
            isTeleporting = false;
            yield break;
        }
        panelImage.DOKill();
        darkPanel.SetActive(true);

        panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);

        yield return panelImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        if (targetDestination != null && playerTransform != null)
        {
            IsInOcean = isGoingToOcean;
            if (!IsInOcean && SoundManager.Instance != null) SoundManager.Instance.StopSFX();
            playerTransform.position = new Vector3(
                targetDestination.position.x,
                targetDestination.position.y,
                playerTransform.position.z
            );

            if (SoundManager.Instance != null)
            {
                if (isGoingToOcean)
                {
                    if (oceanBGM != null) SoundManager.Instance.PlayBGM(oceanBGM);

                    if (seagullSFX != null && seagullCoroutine == null)
                    {
                        seagullCoroutine = StartCoroutine(SeagullRoutine());
                    }
                }
                else
                {
                    if (TimeManager.Instance != null)
                    {
                        if (TimeManager.Instance.CurrentPeriod == TimeManager.TimePeriod.Night)
                        {
                            if (mainMapNightBGM != null) SoundManager.Instance.PlayBGM(mainMapNightBGM);
                        }
                        else
                        {
                            if (mainMapDayBGM != null) SoundManager.Instance.PlayBGM(mainMapDayBGM);
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        yield return panelImage.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        darkPanel.SetActive(false);
        isTeleporting = false;
    }
    private IEnumerator SeagullRoutine()
    {
        while (IsInOcean && !isSeagullPaused)
        {
            float waitTime = Random.Range(minSeagullInterval, maxSeagullInterval);
            yield return new WaitForSeconds(waitTime);

            if (IsInOcean && !isSeagullPaused && SoundManager.Instance != null && seagullSFX != null)
            {
                SoundManager.Instance.PlaySFX(seagullSFX);
            }
        }

        seagullCoroutine = null;
    }
    private void OnDisable()
    {
        StopSeagullSound();
    }
    public void ResumeOceanBGM()
    {
        if (IsInOcean && SoundManager.Instance != null && oceanBGM != null)
        {
            SoundManager.Instance.PlayBGM(oceanBGM);
        }
    }
    public void ResumeOutdoorBGM()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = IsInOcean ? oceanBGM :
            TimeManager.Instance != null && TimeManager.Instance.CurrentPeriod == TimeManager.TimePeriod.Night
                ? mainMapNightBGM : mainMapDayBGM;
        if (clip != null) SoundManager.Instance.PlayBGM(clip);
    }

    public void StopSeagullSound()
    {
        if (seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
        }
    }
}
