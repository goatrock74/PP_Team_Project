using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class MainMenuAnimation : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private RectTransform title;
    [SerializeField] private float titleLandingY = 150f;
    [SerializeField] private float titleDropDistance = 800f;
    [SerializeField] private float titleDropDuration = 0.8f;

    [Header("Camera Shake")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField] private RectTransform mainMenuTransform;

    [Header("Buttons")]
    [SerializeField] private CanvasGroup[] buttons;
    [SerializeField] private float buttonFadeDuration = 0.3f;
    [SerializeField] private float buttonDelay = 0.15f;

    public void PlayAnimation()
    {
        title.DOKill();

        Vector2 landingPosition = new Vector2(
            title.anchoredPosition.x,
            titleLandingY
        );

        title.anchoredPosition = new Vector2(
            landingPosition.x,
            landingPosition.y + titleDropDistance
        );

        foreach (CanvasGroup button in buttons)
        {
            button.DOKill();
            button.alpha = 0f;
            button.interactable = false;
            button.blocksRaycasts = false;
        }

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            title.DOAnchorPos(
                landingPosition,
                titleDropDuration
            )
            .SetEase(Ease.InQuad)
        );

        sequence.AppendCallback(() =>
        {
            cameraTransform.DOShakePosition(
                shakeDuration,
                shakeStrength,
                20,
                90f,
                false,
                true
            );

            mainMenuTransform.DOShakeAnchorPos(
                shakeDuration,
                10f,
                20,
                90f,
                false,
                true
            );
        });

        sequence.AppendInterval(0.1f);

        sequence.AppendCallback(() =>
        {
            ShowButtons();
        });
    }

    private void ShowButtons()
    {
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < buttons.Length; i++)
        {
            CanvasGroup button = buttons[i];

            sequence.Append(
                button.DOFade(1f, buttonFadeDuration)
            );

            sequence.AppendCallback(() =>
            {
                button.interactable = true;
                button.blocksRaycasts = true;
            });

            sequence.AppendInterval(buttonDelay);
        }
    }
}
