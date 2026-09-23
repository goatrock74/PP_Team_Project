using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TriangleFadeInOut : MonoBehaviour
{
    [SerializeField] private Image triangle;
    private void Start()
    {
        Sequence sequence = DOTween.Sequence();

        triangle.DOFade(0f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo) 
            .SetEase(Ease.InOutSine);
    }
}
