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
    private bool isSeagullPaused = false; // 상점 때문에 일시정지되었는지 체크용

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (IsInOcean && SoundManager.Instance != null && oceanBGM != null)
        {
            SoundManager.Instance.PlayBGM(oceanBGM);

            if (seagullSFX != null && seagullCoroutine == null)
            {
                seagullCoroutine = StartCoroutine(SeagullRoutine());
            }
        }
    }

    public void PauseSeagullsForShop()
    {
        if (IsInOcean && seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
            isSeagullPaused = true;
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
            if (seagullCoroutine == null && seagullSFX != null)
            {
                seagullCoroutine = StartCoroutine(SeagullRoutine());
            }
            isSeagullPaused = false;
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
        IsInOcean = isGoingToOcean;
        isSeagullPaused = false; // 맵 이동 시 초기화

        if (!isGoingToOcean && seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
        }

        Image panelImage = darkPanel.GetComponent<Image>();
        panelImage.DOKill();
        darkPanel.SetActive(true);

        panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);

        yield return panelImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        Transform targetDestination = isGoingToOcean ? oceanDestination : mainMapDestination;

        if (targetDestination != null && playerTransform != null)
        {
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
        while (IsInOcean)
        {
            float waitTime = Random.Range(minSeagullInterval, maxSeagullInterval);
            yield return new WaitForSeconds(waitTime);

            if (IsInOcean && SoundManager.Instance != null && seagullSFX != null)
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
    public void StopSeagullSound()
    {
        if (seagullCoroutine != null)
        {
            StopCoroutine(seagullCoroutine);
            seagullCoroutine = null;
        }
    }
}
