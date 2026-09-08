using DG.Tweening;
using System;
using System.Net.NetworkInformation;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UI;

public class UIManger : MonoBehaviour
{
    [Header("BlackPanel")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Panel")]
    [SerializeField] private GameObject buyPanel;
    [SerializeField] private GameObject sellPanel;


    [Header("BT")]
    [SerializeField] private GameObject bt;

    private void Awake()
    {
        StartSetting();
        buyPanel.SetActive(false);
        sellPanel.SetActive(false);
    }

    private void StartSetting()
    {
        canvasGroup.DOFade(0f, 0.5f);
    }

    public void ClickBuyBT()
    {
        bt.SetActive(false); 
        buyPanel.SetActive(true);
    }

    public void ClickSellBT()
    {
        bt.SetActive(false); 
        sellPanel.SetActive(true);
    }

    public void BackToTheMain()
    {
        bt.SetActive(true);
        if(buyPanel == false)
        {
            sellPanel.SetActive(false);
        }
        else
        {
            buyPanel.SetActive(false);
        }
    }
}
