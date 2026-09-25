using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MultiMapPreview : MonoBehaviour
{
    [System.Serializable]
    public struct MapGroup
    {
        public string mapName;            
        public GameObject mapObject;      
        public Transform[] waypoints;     
    }

    [Header("맵 데이터 설정")]
    public MapGroup[] mapGroups;

    [Header("연출 설정")]
    [Tooltip("지점 간 이동 시간(초)")]
    public float travelTime = 4.0f;
    [Tooltip("지점 도착 후 대기 시간(초)")]
    public float waitTime = 1.0f;
    [Tooltip("화면 페이드 전환 시간(초)")]
    public float fadeDuration = 1.0f;

    [Header("UI 페이드 연동")]
    public CanvasGroup fadeCanvasGroup;

    private int currentMapIndex = 0;
    private Vector3 originCameraPosition;
    void Start()
    {
        originCameraPosition = transform.position;
        if (mapGroups != null && mapGroups.Length > 0)
        {
            UpdateMapVisibility(0);
            Vector3 startPos = mapGroups[0].waypoints[0].position;
            transform.position = new Vector3(startPos.x, startPos.y, -10f);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

            StartCoroutine(MainPreviewLoop());
        }
    }

    private IEnumerator MainPreviewLoop()
    {
        while (true)
        {
            MapGroup currentMap = mapGroups[currentMapIndex];

            for (int i = 0; i < currentMap.waypoints.Length - 1; i++)
            {
                Vector3 startPos = transform.position;
                Vector3 targetPos = new Vector3(currentMap.waypoints[i + 1].position.x, currentMap.waypoints[i + 1].position.y, -10f);

                yield return StartCoroutine(MoveCamera(startPos, targetPos));

                if (waitTime > 0f)
                    yield return new WaitForSeconds(waitTime);
            }

            if (fadeCanvasGroup != null)
            {
                yield return fadeCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
            }

            currentMapIndex = (currentMapIndex + 1) % mapGroups.Length;
            UpdateMapVisibility(currentMapIndex);

            Vector3 nextMapStartPos = mapGroups[currentMapIndex].waypoints[0].position;
            transform.position = new Vector3(nextMapStartPos.x, nextMapStartPos.y, -10f);

            if (fadeCanvasGroup != null)
            {
                yield return fadeCanvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();
            }
        }
    }

    private IEnumerator MoveCamera(Vector3 start, Vector3 target)
    {
        float elapsedTime = 0f;
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / travelTime);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
    }

    private void UpdateMapVisibility(int activeIndex)
    {
        for (int i = 0; i < mapGroups.Length; i++)
        {
            if (mapGroups[i].mapObject != null)
            {
                mapGroups[i].mapObject.SetActive(i == activeIndex);
            }
        }
    }
    public void StopPreview()
    {
        StopAllCoroutines();

        if (mapGroups != null)
        {
            foreach (var group in mapGroups)
            {
                if (group.mapObject != null)
                {
                    group.mapObject.SetActive(false);
                }
            }
        }

        transform.position = new Vector3(originCameraPosition.x, originCameraPosition.y, -10f);

        this.enabled = false;
    }
}
