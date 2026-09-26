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
        [Tooltip("이 맵을 비출 때의 카메라 크기 (기본값 보통 5)")]
        public float cameraSize;
    }

    [Header("맵 데이터 설정")]
    public MapGroup[] mapGroups;

    [Header("연출 설정")]
    [Tooltip("지점 간 이동 시간(초)")]
    public float travelTime = 4.0f;
    [Tooltip("지점 도착 후 대기 시간(초) - 마지막 지점에서는 무시됨")]
    public float waitTime = 1.0f;
    [Tooltip("화면 페이드 전환 시간(초)")]
    public float fadeDuration = 1.0f;

    [Header("UI 페이드 연동")]
    public CanvasGroup fadeCanvasGroup;

    private int currentMapIndex = 0;
    private Vector3 originCameraPosition;
    private float originCameraSize;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        originCameraPosition = transform.position;
        originCameraSize = cam.orthographicSize;

        if (mapGroups != null && mapGroups.Length > 0)
        {
            UpdateMapVisibility(0);
            Vector3 startPos = mapGroups[0].waypoints[0].position;

            cam.orthographicSize = mapGroups[0].cameraSize > 0 ? mapGroups[0].cameraSize : originCameraSize;
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

                bool isLastSegment = (i == currentMap.waypoints.Length - 2);

                if (isLastSegment)
                {
                    yield return StartCoroutine(MoveCameraAndFadeOut(startPos, targetPos));
                }
                else
                {
                    yield return StartCoroutine(MoveCamera(startPos, targetPos));

                    if (waitTime > 0f)
                        yield return new WaitForSeconds(waitTime);
                }
            }

            currentMapIndex = (currentMapIndex + 1) % mapGroups.Length;
            MapGroup nextMap = mapGroups[currentMapIndex];

            UpdateMapVisibility(currentMapIndex);

            Vector3 nextMapStartPos = nextMap.waypoints[0].position;
            transform.position = new Vector3(nextMapStartPos.x, nextMapStartPos.y, -10f);

            if (nextMap.cameraSize > 0)
            {
                cam.orthographicSize = nextMap.cameraSize;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.DOFade(0f, fadeDuration);
            }
        }
    }

    private IEnumerator MoveCamera(Vector3 start, Vector3 target)
    {
        float elapsedTime = 0f;
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
    }

    private IEnumerator MoveCameraAndFadeOut(Vector3 start, Vector3 target)
    {
        float elapsedTime = 0f;
        bool fadeStarted = false;

        float fadeStartTime = Mathf.Max(0f, travelTime - fadeDuration);

        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;

            transform.position = Vector3.Lerp(start, target, t);

            if (!fadeStarted && elapsedTime >= fadeStartTime)
            {
                fadeStarted = true;
                if (fadeCanvasGroup != null)
                    fadeCanvasGroup.DOFade(1f, fadeDuration);
            }

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
                    group.mapObject.SetActive(false);
            }
        }

        transform.position = new Vector3(originCameraPosition.x, originCameraPosition.y, -10f);
        if (cam != null) cam.orthographicSize = originCameraSize;
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        this.enabled = false;
    }
}
