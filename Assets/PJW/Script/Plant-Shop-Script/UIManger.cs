using DG.Tweening;
using System;
using System.Net.NetworkInformation;
using TMPro;
using Unity.Properties;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class UIManger : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject buyPanel;
    [SerializeField] GameObject sellPanel;
    [SerializeField] private AudioClip clickaudio;
    [Header("씬 안에서 사용하는 상점 UI")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private bool buyOnly;
    [SerializeField] private GameObject[] shopVisuals;
    [SerializeField] private Transform shopViewAnchor;
    [SerializeField] private GameObject hotbarPanel;
    private Camera shopCamera;
    private bool environmentHidden;
    private bool hotbarWasActive;
    // Reserved for ShopVisual in ProjectSettings/TagManager.asset.
    private const int ShopVisualLayerIndex = 30;

    private void OnEnable()
    {
        if (menuPanel == null && buyPanel == null && sellPanel == null) return;
        ShowInitialPanel();
    }

    private void ShowInitialPanel()
    {
        SetPanels(!buyOnly, buyOnly, false);
    }

    private void OnDisable()
    {
        if (environmentHidden) SetControlsGuideHidden(false);
        SetShopVisuals(false);
        SetShopEnvironment(false);
    }

    private void SetShopEnvironment(bool inside)
    {
        // Standalone shop scenes keep their existing scene transition behavior.
        if (shopRoot == null || environmentHidden == inside) return;
        environmentHidden = inside;
        if (hotbarPanel != null)
        {
            if (inside)
            {
                hotbarWasActive = hotbarPanel.activeSelf;
                hotbarPanel.SetActive(false);
            }
            else hotbarPanel.SetActive(hotbarWasActive);
        }
        if (inside) ShopEntrance.EnterShop();
        else ShopExit.ExitShop();
    }

    private void SetShopVisuals(bool visible)
    {
        if (shopVisuals == null) return;
        foreach (GameObject visual in shopVisuals)
        {
            if (visual != null) visual.SetActive(visible);
        }
        if (shopViewAnchor != null)
        {
            if (visible && shopCamera == null) CreateShopCamera();
            if (shopCamera != null) shopCamera.enabled = visible;
        }
    }

    private void CreateShopCamera()
    {
        int layer = LayerMask.NameToLayer("ShopVisual");
        if (layer < 0)
        {
            // Unity can retain the old layer names while the editor is open.
            // An unnamed layer is still a valid camera culling layer.
            if (!string.IsNullOrEmpty(LayerMask.LayerToName(ShopVisualLayerIndex)))
            {
                Debug.LogError("Layer 30 is occupied. Assign a free layer to ShopVisual.", this);
                return;
            }
            layer = ShopVisualLayerIndex;
        }
        foreach (GameObject visual in shopVisuals)
        {
            if (visual == null) continue;
            foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        GameObject cameraObject = new GameObject("Shop Presentation Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.SetPositionAndRotation(
            shopViewAnchor.position + Vector3.back * 10f, Quaternion.identity);
        shopCamera = cameraObject.AddComponent<Camera>();
        UniversalAdditionalCameraData cameraData = shopCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Base;
        cameraData.SetRenderer(-1);
        cameraData.renderPostProcessing = false;
        shopCamera.orthographic = true;
        shopCamera.orthographicSize = 5f;
        shopCamera.nearClipPlane = 0.1f;
        shopCamera.farClipPlane = 100f;
        shopCamera.clearFlags = CameraClearFlags.SolidColor;
        shopCamera.backgroundColor = Color.black;
        shopCamera.cullingMask = 1 << layer;
        shopCamera.depth = Camera.main != null ? Camera.main.depth + 1f : 1f;
    }

    private void SetPanels(bool menu, bool buy, bool sell)
    {
        SetControlsGuideHidden(menu || buy || sell);
        SetShopEnvironment(menu || buy || sell);
        if (menuPanel != null) menuPanel.SetActive(menu);
        if (buyPanel != null) buyPanel.SetActive(buy);
        if (sellPanel != null) sellPanel.SetActive(sell);
        SetShopVisuals(menu || buy || sell);
    }

    public void OpenShop()
    {
        if (shopRoot != null) shopRoot.SetActive(true);
        ShowInitialPanel();
    }

    // Public instance method so Button.onClick can set the static guide flag.
    public void SetControlsGuideHidden(bool hidden)
    {
        ControlsGuideUI.Hidden = hidden;
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
