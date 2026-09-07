using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopOwner : MonoBehaviour,IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Sprite image_fall;
    [SerializeField] private Sprite image_close;
    private Image imageComp;

    private Tween moveTween;
    private Tween rotateTween;

    private void Awake()
    {
        imageComp = GetComponent<Image>();
    }

    private void Start()
    {
        imageComp.sprite = image_fall;
        StartIdleAnimation();
    }

    private void StartIdleAnimation()
    {
        // 1. 위아래로 부드럽게 왕복하는 애니메이션 (Y축)
        moveTween = transform.DOLocalMoveY(transform.localPosition.y + 15f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 2. 미세하게 좌우로 기웃거리는 회전 애니메이션 (Z축)
        rotateTween = transform.DORotate(new Vector3(0, 0, 2.5f), 0.5f * 1.3f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        imageComp.sprite = image_fall;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        imageComp.sprite = image_close;
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        rotateTween?.Kill();
    }
}
