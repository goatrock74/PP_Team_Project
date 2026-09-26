using System.Collections;
using UnityEngine;

public class Camera2DPreview : MonoBehaviour
{
    [Header("카메라가 방문할 지점들")]
    public Transform[] waypoints;

    [Header("연출 설정")]
    [Tooltip("지점 간 이동에 걸리는 시간 (초)")]
    public float travelTime = 5.0f;

    [Tooltip("지점에 도착했을 때 멈춰서 기다릴 시간 (0이면 멈추지 않고 계속 이동)")]
    public float waitTime = 0.0f;

    private int currentIndex = 0;

    void Start()
    {
        if (waypoints.Length > 0)
        {
            transform.position = new Vector3(waypoints[0].position.x, waypoints[0].position.y, -10f);
            StartCoroutine(MoveSequence());
        }
    }

    private IEnumerator MoveSequence()
    {
        while (true)
        {
            int nextIndex = (currentIndex + 1) % waypoints.Length;

            Vector3 startPos = transform.position;
            Vector3 targetPos = new Vector3(waypoints[nextIndex].position.x, waypoints[nextIndex].position.y, -10f);

            float elapsedTime = 0f;

            while (elapsedTime < travelTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / travelTime;


                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            currentIndex = nextIndex;

            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}
