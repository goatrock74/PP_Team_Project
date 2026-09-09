using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopOwner : MonoBehaviour
{
    [SerializeField] private Sprite image_fall;
    [SerializeField] private Sprite image_close;

    private SpriteRenderer spriteRender;
    private DG.Tweening.Sequence idleSequence;

    private Vector3 startPosition;
    private Vector3 startRotation;

    private void Awake()
    {
        spriteRender = GetComponent<SpriteRenderer>();

        // 시작 월드 좌표 저장
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
    }

    private void Start()
    {
        spriteRender.sprite = image_fall;
        StartIdleAnimation();
    }

    private void StartIdleAnimation()
    {
        // 이전 애니메이션 정리
        idleSequence?.Kill();

        idleSequence = DOTween.Sequence();

        // =========================
        // 1. 위아래 이동 - 월드 좌표
        // =========================

        Tween moveTween = transform.DOMoveY(
            startPosition.y + 0.3f,
            0.5f
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);


        // =========================
        // 2. 좌우로 기웃거리기 - 월드 회전
        // =========================

        Tween rotateTween = transform.DORotate(
            new Vector3(
                startRotation.x,
                startRotation.y,
                startRotation.z + 2.5f
            ),
            0.65f
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);


        // 동시에 실행
        idleSequence.Join(moveTween);
        idleSequence.Join(rotateTween);
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 마우스 화면 좌표
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            // 화면 좌표 → 월드 좌표
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    -Camera.main.transform.position.z
                )
            );

            // 해당 위치에 Collider2D가 있는지 검사
            RaycastHit2D hit = Physics2D.Raycast(
                mouseWorldPosition,
                Vector2.zero
            );

            if (hit.collider == null)
                return;

            if (hit.collider.gameObject != gameObject)
                return;

            StartCoroutine(OnMouseClickEnter());
        }
    }

    IEnumerator OnMouseClickEnter()
    {
        spriteRender.sprite = image_close;
        yield return new WaitForSeconds(0.3f);
        spriteRender.sprite = image_fall;
    }

    private void OnDestroy()
    {
        idleSequence?.Kill();
    }
}
