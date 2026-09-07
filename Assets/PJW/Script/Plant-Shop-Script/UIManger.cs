using DG.Tweening;
using System.Net.NetworkInformation;
using Unity.Properties;
using UnityEngine;

public class UIManger : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        StartSetting();
    }

    private void StartSetting()
    {
        canvasGroup.DOFade(0f, 0.5f);
    }
}
