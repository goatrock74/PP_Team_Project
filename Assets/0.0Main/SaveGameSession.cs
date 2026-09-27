using System;
using KSM._00.Scripts;
using KSM._00.Scripts.Crop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(2000)]
public class SaveGameSession : MonoBehaviour
{
    private static SaveGameSession instance;
    private float nextSave;
    private bool restoring;
    public static bool IsPlaying => SaveSlotStore.Active != null && SceneManager.GetActiveScene().name != "0.0MainMenu_F";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (instance == null) new GameObject("Save Game Session").AddComponent<SaveGameSession>();
    }
    private void Awake() { instance = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += OnLoaded; }
    private void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; if (instance == this) instance = null; }
    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "0.0MainMenu_F") { Inventorysavemanger.Instance?.DetachForSlotChange(); SaveSlotStore.Deselect(); return; }
        if (!IsPlaying) return;
        var slot = SaveSlotStore.Active;
        TimeManager.Instance?.RestoreSave(slot.day, slot.dayFraction);
        MasteryManager.Instance?.LoadJson(SaveSlotStore.GetString("Mastery"));
        var weather = FindFirstObjectByType<SeasonGrowthAdapter>();
        if (weather != null) weather.IsRaining = SaveSlotStore.GetInt("Rain." + scene.name) != 0;
        if (restoring && slot.hasPosition && scene.name == slot.scene)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) { player.transform.position = slot.position; var rb = player.GetComponent<Rigidbody2D>(); if (rb != null) rb.position = slot.position; }
        }
        restoring = false;
        nextSave = Time.unscaledTime + 3f;
    }
    public static void Play(string id)
    {
        var slot = SaveSlotStore.Read(id);
        if (!Application.CanStreamedLevelBeLoaded(slot.scene)) throw new InvalidOperationException("저장된 씬이 빌드 목록에 없습니다: " + slot.scene);
        Inventorysavemanger.Instance?.DetachForSlotChange();
        SaveSlotStore.Select(id);
        PlayerWallet.Instance.RestoreSave(slot.money);
        instance.restoring = true;
        Time.timeScale = 1;
        SoundManager.Instance?.StopBGM();
        SceneManager.LoadScene(slot.scene);
    }
    private void Update()
    {
        if (!IsPlaying || restoring) return;
        SaveSlotStore.Active.playSeconds += Time.unscaledDeltaTime;
        if (Time.unscaledTime >= nextSave || (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame))
        { SaveNow(); nextSave = Time.unscaledTime + 3f; }
    }
    public static bool SaveNow()
    {
        if (!IsPlaying || instance.restoring) return true;
        try
        {
            if (Inventorysavemanger.Instance != null && !Inventorysavemanger.Instance.IsReady)
                throw new InvalidOperationException("인벤토리 복원이 완료되지 않아 저장을 보류했습니다.");
            var farm = FindFirstObjectByType<FarmSaveManager>();
            if (farm != null && !farm.IsReady) throw new InvalidOperationException("밭 복원이 완료되지 않아 저장을 보류했습니다.");
            Inventorysavemanger.Instance?.SaveInventory(true);
            if (Inventorysavemanger.Instance != null && !Inventorysavemanger.Instance.IsReady)
                throw new InvalidOperationException("인벤토리 저장 실패로 기존 파일을 보호합니다.");
            FarmSaveManager.SaveNow();
            var slot = SaveSlotStore.Active;
            slot.money = PlayerWallet.Instance.CurrentMoney;
            if (TimeManager.Instance != null) { slot.day = TimeManager.Instance.CurrentDay; slot.dayFraction = TimeManager.Instance.DayFraction; }
            if (MasteryManager.Instance != null) SaveSlotStore.SetString("Mastery", MasteryManager.Instance.ToJson());
            slot.scene = SceneManager.GetActiveScene().name;
            var weather = FindFirstObjectByType<SeasonGrowthAdapter>();
            if (weather != null) SaveSlotStore.SetInt("Rain." + slot.scene, weather.IsRaining ? 1 : 0);
            var player = FindFirstObjectByType<PlayerMovement>();
            slot.hasPosition = player != null;
            if (player != null) slot.position = player.transform.position;
            SaveSlotStore.Write(slot);
            return true;
        }
        catch (Exception e) { SaveSlotStore.Report(e); return false; }
    }
    public static void LoadScene(int index)
    {
        if (!SaveNow()) return;
        SceneManager.LoadScene(index);
    }
    private void OnApplicationPause(bool pause) { if (pause) SaveNow(); }
    private void OnApplicationQuit() => SaveNow();
}
