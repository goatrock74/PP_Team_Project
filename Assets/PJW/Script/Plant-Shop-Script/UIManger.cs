using DG.Tweening;
using System;
using System.Net.NetworkInformation;
using TMPro;
using Unity.Properties;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManger : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject buyPanel;
    [SerializeField] GameObject sellPanel;

    private void Awake()
    {
        buyPanel.SetActive(false);
        sellPanel.SetActive(false);
    }

    // BUY 버튼에 연결
    public void OpenBuyPanel()
    {
        menuPanel.SetActive(false);
        buyPanel.SetActive(true);
        sellPanel.SetActive(false);
    }

    // SELL 버튼에 연결
    public void OpenSellPanel()
    {
        menuPanel.SetActive(false);
        buyPanel.SetActive(false);
        sellPanel.SetActive(true);
    }

    // 다시 메뉴로 돌아가고 싶을 때
    public void BackToMenu()
    {
        menuPanel.SetActive(true);
        buyPanel.SetActive(false);
        sellPanel.SetActive(false);
    }
}
