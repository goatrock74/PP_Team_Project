using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextFade : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float duration = 1.3f;
    public Tween Textfade()
    {
        text.alpha = 0;
        Debug.Log("페이드");
        return text.DOFade(1f, duration)
            .SetEase(Ease.OutQuad);
    }
}
