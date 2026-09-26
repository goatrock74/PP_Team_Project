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
    [SerializeField] private Transform oceanDestination;       // 바다 도착 위치
    [SerializeField] private Transform mainMapDestination;     // 메인 맵으로 돌아올 위치

    [Header("페이드 연출 UI")]
    [SerializeField] private GameObject darkPanel;
    [SerializeField] private float fadeDuration = 1.0f;

    private bool isTeleporting = false;

    // 바다로 이동하는 버튼에 연결
    public void TeleportToOcean()
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(true));
    }

    // 메인 맵으로 돌아오는 버튼에 연결
    public void TeleportToMainMap()
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportRoutine(false));
    }

    private IEnumerator TeleportRoutine(bool isGoingToOcean)
    {
        isTeleporting = true;

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
        }
        else
        {
            Debug.LogWarning("도착 지점이나 플레이어가 지정되지 않았습니다!");
        }

        yield return new WaitForSeconds(1.5f);

        yield return panelImage.DOFade(0f, fadeDuration).SetEase(Ease.Linear).WaitForCompletion();

        darkPanel.SetActive(false);
        isTeleporting = false;
    }
}
