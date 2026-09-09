using DG.Tweening;
using TMPro;
using UnityEngine;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textToBlink;
    [SerializeField] private float duration = 0.8f;

    private Tween blinkTween;

    private void OnEnable()
    {
        StartBlinking();
    }

    private void StartBlinking()
    {
        textToBlink.alpha = 1f;
        blinkTween = textToBlink.DOFade(0f, duration)
            .SetLoops(-1, LoopType.Yoyo) 
            .SetEase(Ease.InOutSine);   
    }

    private void OnDisable()
    {
        blinkTween?.Kill();
    }
}
