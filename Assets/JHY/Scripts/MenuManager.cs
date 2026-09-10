using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
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
    private bool isStartScreen = true;
    private bool isTransitioning = false;

    private void Start()
    {
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
                startScreen.alpha = 0f;
                startScreen.interactable = false;
                startScreen.blocksRaycasts = false;

                mainMenu.alpha = 1f;
                mainMenu.interactable = true;
                mainMenu.blocksRaycasts = true;

                isStartScreen = false;
                particle.SetActive(false);
                background.SetActive(false);

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
        SceneManager.LoadScene("JHY");
    }
}