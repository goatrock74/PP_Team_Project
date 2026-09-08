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

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isStartScreen = true;
    private bool isTransitioning = false;

    private void Start()
    {
        // 시작 화면
        startScreen.alpha = 1f;
        startScreen.interactable = true;
        startScreen.blocksRaycasts = true;

        // 메인 메뉴 숨기기
        mainMenu.alpha = 0f;
        mainMenu.interactable = false;
        mainMenu.blocksRaycasts = false;

        // 검은 화면에서 시작
        fadePanel.alpha = 1f;

        // 페이드 인
        fadePanel
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InOutQuad);
    }

    private void Update()
    {
        // 시작 화면일 때만 클릭 감지
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

        // 페이드 아웃
        fadePanel
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // 시작 화면 숨김
                startScreen.alpha = 0f;
                startScreen.interactable = false;
                startScreen.blocksRaycasts = false;

                // 메인 메뉴 표시
                mainMenu.alpha = 1f;
                mainMenu.interactable = true;
                mainMenu.blocksRaycasts = true;

                // 이제 시작 화면이 아님
                isStartScreen = false;

                // 페이드 인
                fadePanel
                    .DOFade(0f, fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        isTransitioning = false;
                    });
            });
    }
    public void NextScene()
    {
        Debug.Log("NextScene"); 
        SceneManager.LoadScene("JHY");
    }
}