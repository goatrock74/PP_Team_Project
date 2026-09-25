using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Camera Preview 연출")]
    [SerializeField] private MultiMapPreview mapPreview; // 카메라 연출 스크립트 연결

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup startScreen;
    [SerializeField] private CanvasGroup mainMenu;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private GameObject particle;
    [SerializeField] private GameObject background;
    [SerializeField] private MainMenuAnimation mainMenuAnimation;
    [SerializeField] private GameObject settingPanel;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Sound")]
    [SerializeField] private AudioClip clickMain;
    [SerializeField] private AudioClip MainBGM;

    private bool isStartScreen = true;
    private bool isTransitioning = false;

    private void Start()
    {
        SoundManager.Instance.PlayBGM(MainBGM);
        startScreen.alpha = 1f;
        startScreen.interactable = true;
        startScreen.blocksRaycasts = true;

        mainMenu.alpha = 0f;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;

        fadePanel.alpha = 1f;

        fadePanel
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InOutQuad);
    }

    private void Update()
    {
        if (!isStartScreen)
            return;

        if (isTransitioning)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            SoundManager.Instance.PlaySFX(clickMain);
            ShowMainMenu();
        }
    }

    private void ShowMainMenu()
    {
        isTransitioning = true;

        fadePanel
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                if (mapPreview != null)
                {
                    mapPreview.StopPreview();
                }

                startScreen.alpha = 0f;
                startScreen.interactable = false;
                startScreen.blocksRaycasts = false;

                mainMenu.alpha = 1f;
                mainMenu.interactable = true;
                mainMenu.blocksRaycasts = true;

                isStartScreen = false;
                //particle.SetActive(true);
                background.SetActive(true);

                mainMenuAnimation.PlayAnimation();

                fadePanel
                    .DOFade(0f, fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        isTransitioning = false;
                    });
            });
    }

    public void SettingOpen()
    {
        settingPanel.SetActive(true);
    }

    public void SettingClose()
    {
        settingPanel.SetActive(false);
    }

    public void NextScene()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        SoundManager.Instance.PlaySFX(clickMain);

        fadePanel
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                SoundManager.Instance.StopBGM();
                SceneManager.LoadScene(1);
            });
    }
}