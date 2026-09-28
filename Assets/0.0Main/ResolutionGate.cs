using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Build entry scene: do not load gameplay until the desktop resolution is valid.
public sealed class ResolutionGate : MonoBehaviour
{
    public const string SceneName = "ResolutionCheck";
    [SerializeField] private int requiredWidth = 1920;
    [SerializeField] private int requiredHeight = 1080;
    [SerializeField] private Font messageFont;
    private Text message;
    private bool loading;

    private void Awake()
    {
        var cameraObject = new GameObject("Black Background", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.depth = 100;

        var canvasObject = new GameObject("Resolution Notice", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var textObject = new GameObject("Message", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.1f);
        rect.anchorMax = new Vector2(0.92f, 0.9f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        message = textObject.GetComponent<Text>();
        message.horizontalOverflow = HorizontalWrapMode.Overflow;
        message.font = messageFont;
        message.color = Color.red;
        message.alignment = TextAnchor.MiddleCenter;
        message.fontSize = 40;
        message.resizeTextForBestFit = true;
        message.resizeTextMinSize = 14;
        message.resizeTextMaxSize = 40;
        message.raycastTarget = false;
    }

    private void Start()
    {
        SoundManager.Instance?.StopBGM();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (loading) return;
        // Desktop resolution, not the size of the game window.
        var resolution = Screen.currentResolution;
        if (resolution.width == requiredWidth && resolution.height == requiredHeight)
        {
            loading = true;
            SceneManager.LoadScene("0.0MainMenu_F");
            return;
        }

        message.text = $"컴퓨터의 해상도를 {requiredWidth} × {requiredHeight}(으)로 변경해 주세요.\n\n"
            + $"현재 해상도: {resolution.width} × {resolution.height}\n\n"
            + "해상도를 변경하면 자동으로 시작합니다.\n\n화면을 클릭하면 게임이 종료됩니다.";

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            loading = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
