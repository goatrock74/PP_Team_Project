using DG.Tweening;
using System;
using System.Net.NetworkInformation;
using TMPro;
using Unity.Properties;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManger : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject buyPanel;
    [SerializeField] GameObject sellPanel;
    [SerializeField] private AudioClip clickaudio;
    [Header("씬 안에서 사용하는 상점 UI")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private bool buyOnly;

    private void OnEnable()
    {
        if (menuPanel == null && buyPanel == null && sellPanel == null) return;
        ShowInitialPanel();
    }

    private void ShowInitialPanel()
    {
        SetPanels(!buyOnly, buyOnly, false);
    }

    private void SetPanels(bool menu, bool buy, bool sell)
    {
        if (menuPanel != null) menuPanel.SetActive(menu);
        if (buyPanel != null) buyPanel.SetActive(buy);
        if (sellPanel != null) sellPanel.SetActive(sell);
    }

    public void OpenShop()
    {
        if (shopRoot != null) shopRoot.SetActive(true);
        ShowInitialPanel();
    }

    public void CloseShop()
    {
        SetPanels(false, false, false);
        if (shopRoot != null) shopRoot.SetActive(false);
    }

    public void BackOrClose()
    {
        if (buyOnly || (menuPanel != null && menuPanel.activeSelf)) CloseShop();
        else BackToMenu();
    }


    // BUY 버튼에 연결
    public void OpenBuyPanel()
    {
        SetPanels(false, true, false);
    }

    // SELL 버튼에 연결
    public void OpenSellPanel()
    {
        if (buyOnly || sellPanel == null) return;
        SetPanels(false, false, true);
    }

    // 다시 메뉴로 돌아가고 싶을 때
    public void BackToMenu()
    {
        if (buyOnly) CloseShop();
        else SetPanels(true, false, false);
    }

    public void OpenOnlyBuyPanel()
    {
        OpenBuyPanel();
    }

    public void CloseBuyPaenl()
    {
        CloseShop();
    }

    public void InShop(int sceneNum)
    {
        if (shopRoot != null) OpenShop();
        else SceneManager.LoadScene(sceneNum);
    }

    public void OutShop(int sceneNum)
    {
        if (shopRoot != null) CloseShop();
        else SceneManager.LoadScene(sceneNum);
    }

    public void ClickSound()
    {
        SoundManager.Instance.PlaySFX(clickaudio);
    }

}
