using System.Text;
using UnityEditor;
using UnityEngine;

public static class SaveSceneDiagnostics
{
    private static int testStep;
    private static double nextStep;
    private static string testSlot;
    private static Vector3 expectedPosition;
    private static string expectedInventory;
    private static string failure;
    private static bool running;

    [MenuItem("Tools/Save System/Run Save Reload Smoke Test %#F10")]
    public static void RunSmokeTest()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before running this test.");
        SessionState.SetBool("SaveTest.RunSmoke", true);
        PlayMainMenu();
    }

    private static void TickTest()
    {
        if (!running || EditorApplication.timeSinceStartup < nextStep) return;
        nextStep = EditorApplication.timeSinceStartup + 5;
        try
        {
            if (failure != null) throw new System.Exception(failure);
            switch (testStep++)
            {
                case 0:
                    testSlot = SaveSlotStore.Create().id;
                    SessionState.SetString("SaveTest.CleanupSlot", testSlot);
                    SaveGameSession.Play(testSlot);
                    break;
                case 1:
                    PlayerWallet.Instance.AddMoney(137);
                    TimeManager.Instance.RestoreSave(8, .25f);
                    var player = Object.FindFirstObjectByType<KSM._00.Scripts.PlayerMovement>();
                    if (player == null) throw new System.Exception("Player missing");
                    expectedPosition = player.transform.position + new Vector3(.25f, 0, 0);
                    player.transform.position = expectedPosition;
                    var rb = player.GetComponent<Rigidbody2D>(); if (rb != null) rb.position = expectedPosition;
                    if (!SaveGameSession.SaveNow()) throw new System.Exception("Save failed: " + SaveSlotStore.LastError);
                    expectedInventory = SaveSlotStore.GetString(Inventorysavemanger.SaveKey);
                    if (string.IsNullOrEmpty(expectedInventory)) throw new System.Exception("Inventory not captured");
                    SaveGameSession.LoadScene(0);
                    break;
                case 2:
                    if (SaveSlotStore.Active != null) throw new System.Exception("Slot remained active in menu");
                    SaveGameSession.Play(testSlot);
                    break;
                case 3:
                    if (PlayerWallet.Instance.CurrentMoney != 137) throw new System.Exception("Wallet did not restore");
                    if (TimeManager.Instance.CurrentDay != 8) throw new System.Exception("Day did not restore");
                    var clock = Object.FindFirstObjectByType<SeasonGrowthAdapter>();
                    if (clock == null || clock.TotalGameDays < 7) throw new System.Exception("Crop clock lost persistent TimeManager");
                    var loaded = Object.FindFirstObjectByType<KSM._00.Scripts.PlayerMovement>();
                    if (loaded == null || Vector3.Distance(loaded.transform.position, expectedPosition) > .05f) throw new System.Exception("Position did not restore");
                    if (SaveSlotStore.GetString(Inventorysavemanger.SaveKey) != expectedInventory) throw new System.Exception("Inventory did not restore");
                    SaveGameSession.LoadScene(0);
                    break;
                case 4:
                    System.IO.File.WriteAllText("Temp/SaveSmokeResult.txt", "PASS: new slot, capture, menu return, reload, money, day, position, inventory, second menu return; no runtime errors.");
                    FinishTest();
                    break;
            }
        }
        catch (System.Exception e)
        {
            System.IO.File.WriteAllText("Temp/SaveSmokeResult.txt", "FAIL step " + (testStep - 1) + ": " + e);
            FinishTest();
        }
    }
    private static void CaptureError(string message, string trace, LogType type)
    {
        if (running && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) failure = message + "\n" + trace;
    }
    private static void FinishTest()
    {
        running = false;
        EditorApplication.update -= TickTest;
        Application.logMessageReceived -= CaptureError;
        SessionState.SetString("SaveTest.CleanupSlot", testSlot ?? "");
        EditorApplication.isPlaying = false;
    }
    [MenuItem("Tools/Save System/Play Main Menu Only %#F9")]
    public static void PlayMainMenu()
    {
        var previous = UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
        SessionState.SetString("SaveTest.PreviousStartScene", previous == null ? "" : AssetDatabase.GetAssetPath(previous));
        SessionState.SetBool("SaveTest.RestoreStartScene", true);
        UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/0.0Main/0.0MainMenu_F.unity");
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Tools/Save System/Write Scene Diagnostics")]
    public static void Dump()
    {
        var text = new StringBuilder();
        text.AppendLine("Playing=" + EditorApplication.isPlaying + " Paused=" + EditorApplication.isPaused);
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            text.AppendLine($"CAMERA {Path(camera.transform)} active={camera.isActiveAndEnabled} depth={camera.depth} scene={camera.gameObject.scene.name}");
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            text.AppendLine($"LISTENER {Path(listener.transform)} active={listener.isActiveAndEnabled}");
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            text.AppendLine($"CANVAS {Path(canvas.transform)} active={canvas.isActiveAndEnabled} mode={canvas.renderMode} camera={canvas.worldCamera}");
        System.IO.File.WriteAllText("Temp/SaveSceneDiagnostics.txt", text.ToString());
    }
    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    [InitializeOnLoadMethod] private static void Register()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SaveTest.RunSmoke", false))
            {
                SessionState.SetBool("SaveTest.RunSmoke", false);
                testStep = 0; failure = null; running = true; testSlot = null;
                nextStep = EditorApplication.timeSinceStartup + 3;
                Application.logMessageReceived += CaptureError;
                EditorApplication.update += TickTest;
            }
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                running = false;
                EditorApplication.update -= TickTest;
                Application.logMessageReceived -= CaptureError;
                string cleanup = SessionState.GetString("SaveTest.CleanupSlot", "");
                if (!string.IsNullOrEmpty(cleanup))
                {
                    SaveSlotStore.Deselect(); SaveSlotStore.Delete(cleanup);
                    SessionState.EraseString("SaveTest.CleanupSlot");
                }
            }
            if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("SaveTest.RestoreStartScene", false)) return;
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("SaveTest.PreviousStartScene", ""));
            SessionState.SetBool("SaveTest.RestoreStartScene", false);
        };
    }
}
