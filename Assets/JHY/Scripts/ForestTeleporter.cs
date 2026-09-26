using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ForestTeleporter : MonoBehaviour
{
    [Header("이동 대상")]
    [SerializeField] private Transform playerTransform;

    [Header("목적지 설정")]
    [SerializeField] private Transform mainMapDestination;

    [Header("계절별 숲 도착 위치")]
    [SerializeField] private Transform springForest;
    [SerializeField] private Transform summerForest;
    [SerializeField] private Transform autumnForest;
    [SerializeField] private Transform winterForest;

    [Header("페이드 연출 UI")]
    [SerializeField] private GameObject darkPanel;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("대화 매니저 연결 (추가됨)")]
    [SerializeField] private DialogueManager dialogueManager;

    private bool isTeleporting = false;

    public void TeleportToForest()
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(true));
    }

    public void TeleportToMainMap()
    {
        Debug.Log("ㅇㅇㅇ");
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(false));
    }

    private IEnumerator TeleportRoutine(bool isGoingToForest)
    {
        isTeleporting = true;

        // 이동 시작 시 대화창 안전하게 종료
        if (dialogueManager != null)
        {
            dialogueManager.EndDialogue();
        }

        Image panelImage = darkPanel.GetComponent<Image>();
        panelImage.DOKill();
        darkPanel.SetActive(true);

        panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0f);

        yield return panelImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        Transform targetDestination = null;

        if (isGoingToForest)
        {
            if (TimeManager.Instance != null)
            {
                switch (TimeManager.Instance.CurrentSeason)
                {
                    case TimeManager.SeasonPeriod.Spring: targetDestination = springForest; break;
                    case TimeManager.SeasonPeriod.Summer: targetDestination = summerForest; break;
                    case TimeManager.SeasonPeriod.Autumn: targetDestination = autumnForest; break;
                    case TimeManager.SeasonPeriod.Winter: targetDestination = winterForest; break;
                }
            }
        }
        else
        {
            targetDestination = mainMapDestination;
        }

        if (targetDestination != null && playerTransform != null)
        {
            playerTransform.position = new Vector3(
                targetDestination.position.x,
                targetDestination.position.y,
                playerTransform.position.z
            );
        }
        else
        {
            Debug.LogWarning("도착 지점이나 플레이어가 지정되지 않았습니다!");
        }

        yield return new WaitForSeconds(0.5f);

        yield return panelImage.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        darkPanel.SetActive(false);
        isTeleporting = false;
    }
}
