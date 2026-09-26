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
        [Header("페이드 설정")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.08f;
        [SerializeField, Min(0f)] private float outsideSpawnGap = 0.75f;

        private Rigidbody2D playerBody;
        private PlayerInput playerInput;
        private Collider2D enterDoor;
        private Collider2D outDoor;
        private Transform enterPoint;
        private CanvasGroup fadeGroup;

        private bool isTransitioning;

        private void Awake()
        {
            playerBody = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();

            GameObject fishShopOut = GameObject.Find("FishShopOut");
            GameObject inFishShop = GameObject.Find("InFishShop");

            if (fishShopOut != null)
                enterDoor = fishShopOut.transform.Find("EnterDoor")?.GetComponent<Collider2D>();

            if (inFishShop != null)
            {
                enterPoint = inFishShop.transform.Find("EnterPoint");
                outDoor = inFishShop.transform.Find("OutDoor")?.GetComponent<Collider2D>();
            }

            if (enterDoor == null || outDoor == null || enterPoint == null)
            {
                Debug.LogError(
                    "[FishShopDoorTransition] FishShopOut/EnterDoor 또는 " +
                    "InFishShop/EnterPoint, OutDoor를 찾지 못했습니다.",
                    this
                );
                enabled = false;
                return;
            }

            enterDoor.isTrigger = true;
            outDoor.isTrigger = true;

            CreateFadeOverlay();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!enabled || isTransitioning)
                return;

            if (other == enterDoor)
            {
                StartCoroutine(TeleportRoutine(enterPoint.position));
                return;
            }

            if (other == outDoor)
                StartCoroutine(TeleportRoutine(GetOutsideSpawnPosition()));
        }

        private Vector2 GetOutsideSpawnPosition()
        {
            Bounds doorBounds = enterDoor.bounds;
            return new Vector2(
                doorBounds.center.x,
                doorBounds.min.y - outsideSpawnGap
            );
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
