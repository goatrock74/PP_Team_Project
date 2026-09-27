using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PJH._01.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FishShopDoorTransition : MonoBehaviour
    {
        [Serializable]
        private sealed class DoorLink
        {
            [Tooltip("플레이어가 들어갈 문 콜라이더")]
            public Collider2D door;

            [Tooltip("문을 통과한 뒤 플레이어가 나타날 위치")]
            public Transform destination;

            [Tooltip("도착 위치에서 추가로 이동할 거리")]
            public Vector2 destinationOffset;
        }

        [Header("문 연결 목록")]
        [SerializeField] private DoorLink[] doorLinks;

        [Header("페이드 설정")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.08f;

        private Rigidbody2D playerBody;
        private PlayerInput playerInput;
        private CanvasGroup fadeGroup;

        private bool isTransitioning;

        private void Awake()
        {
            playerBody = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();

            if (doorLinks == null || doorLinks.Length == 0)
            {
                Debug.LogError("[FishShopDoorTransition] 문 연결 목록이 비어 있습니다.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < doorLinks.Length; i++)
            {
                DoorLink link = doorLinks[i];
                if (link == null || link.door == null || link.destination == null)
                {
                    Debug.LogError($"[FishShopDoorTransition] Door Links의 {i}번 연결이 비어 있습니다.", this);
                    enabled = false;
                    return;
                }

                link.door.isTrigger = true;
            }

            CreateFadeOverlay();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled || isTransitioning)
                return;

            foreach (DoorLink link in doorLinks)
            {
                if (other != link.door)
                    continue;

                Vector2 destination = (Vector2)link.destination.position + link.destinationOffset;
                StartCoroutine(TeleportRoutine(destination));
                return;
            }
        }

        private IEnumerator TeleportRoutine(Vector2 destination)
        {
            isTransitioning = true;

            bool reactivateInput = playerInput != null && playerInput.inputIsActive;
            if (reactivateInput)
                playerInput.DeactivateInput();

            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;

            yield return FadeTo(1f);

            playerBody.position = destination;
            playerBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            if (blackHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(blackHoldDuration);

            yield return FadeTo(0f);

            if (reactivateInput)
                playerInput.ActivateInput();

            // 목적지 문과 같은 프레임에 다시 충돌하는 것을 막는다.
            yield return new WaitForFixedUpdate();
            isTransitioning = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeGroup == null)
                yield break;

            float startAlpha = fadeGroup.alpha;
            if (fadeDuration <= 0f)
            {
                fadeGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
        }

        private void CreateFadeOverlay()
        {
            GameObject canvasObject = new GameObject(
                "FishShopTransitionCanvas",
                typeof(Canvas),
                typeof(CanvasScaler)
            );

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject fadeObject = new GameObject(
                "FadeImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup)
            );
            fadeObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = fadeObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            fadeGroup = fadeObject.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
        }
    }
}
