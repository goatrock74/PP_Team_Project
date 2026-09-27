using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Gameplay preferences are staged here; one complete snapshot is committed by SaveGameSession.
public static class SaveSlotStore
{
    [Serializable] public class Entry { public string key; public string value; }
    [Serializable] public class Slot
    {
        public int version = 1;
        public string id;
        public string savedAt;
        public int day = 1;
        public float dayFraction;
        public int money = 0;
        public double playSeconds;
        public string scene = "0.1BaseScene";
        public bool hasPosition;
        public Vector3 position;
        public List<Entry> entries = new List<Entry>();
        [NonSerialized] public bool unavailable;
    }
    public static Slot Active { get; private set; }
    public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "SaveSlots");
    public static string LastError { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Active = null; LastError = null; }
    private static string PathFor(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("Invalid slot ID");
        return Path.Combine(DirectoryPath, id + ".json");
    }
    public static List<Slot> List()
    {
        Directory.CreateDirectory(DirectoryPath);
        var result = new List<Slot>();
        var ids = new HashSet<string>();
        foreach (string path in Directory.GetFiles(DirectoryPath, "*.json")) ids.Add(Path.GetFileNameWithoutExtension(path));
        foreach (string path in Directory.GetFiles(DirectoryPath, "*.json.bak")) ids.Add(Path.GetFileName(path).Replace(".json.bak", ""));
        foreach (string id in ids)
        {
            try { result.Add(Read(id)); }
            catch (Exception e)
            {
                Debug.LogWarning("Save slot unavailable: " + e.Message);
                if (Guid.TryParseExact(id, "N", out _)) result.Add(new Slot { id = id, unavailable = true });
            }
        }
        result.Sort((a, b) => string.CompareOrdinal(b.savedAt, a.savedAt));
        return result;
    }
    private static Slot Decode(string path, string id)
    {
        var slot = JsonUtility.FromJson<Slot>(File.ReadAllText(path));
        if (slot == null || slot.version != 1 || slot.id != id || slot.entries == null || slot.day < 1)
            throw new InvalidDataException("Unsupported or damaged save slot");
        if (string.IsNullOrWhiteSpace(slot.scene) || slot.money < 0 ||
            float.IsNaN(slot.dayFraction) || slot.dayFraction < 0 || slot.dayFraction >= 1 ||
            double.IsNaN(slot.playSeconds) || double.IsInfinity(slot.playSeconds) || slot.playSeconds < 0)
            throw new InvalidDataException("Invalid save metadata");
        var keys = new HashSet<string>();
        foreach (var entry in slot.entries)
            if (entry == null || string.IsNullOrEmpty(entry.key) || !keys.Add(entry.key))
                throw new InvalidDataException("Invalid or duplicate save entry");
        return slot;
    }
    public static Slot Read(string id)
    {
        string path = PathFor(id);
        try { return Decode(path, id); }
        catch { return Decode(path + ".bak", id); }
    }
    public static Slot Create()
    {
        var slot = new Slot { id = Guid.NewGuid().ToString("N") };
        Write(slot);
        return slot;
    }
    public static void Select(string id) { Active = Read(id); }
    public static void Deselect() { Active = null; }
    public static void Delete(string id)
    {
        if (Active != null && Active.id == id) throw new InvalidOperationException("현재 플레이 중인 저장은 삭제할 수 없습니다.");
        string path = PathFor(id);
        foreach (string suffix in new[] { ".bak", ".tmp", "" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }
    public static void Write(Slot slot)
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = PathFor(slot.id);
        slot.savedAt = DateTime.UtcNow.ToString("O");
        string json = JsonUtility.ToJson(slot, true);
        using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path))
        {
            bool validPrimary;
            try { Decode(path, slot.id); validPrimary = true; }
            catch (Exception e) when (e is ArgumentException || e is InvalidDataException || e is IOException) { validPrimary = false; }
            // A recovered slot must never replace its healthy backup with the broken primary.
            File.Replace(path + ".tmp", path, validPrimary ? path + ".bak" : null);
        }
        else File.Move(path + ".tmp", path);
        LastError = null;
    }
    public static bool HasKey(string key) => Active != null ? Active.entries.Exists(e => e.key == key) : PlayerPrefs.HasKey(key);
    public static string GetString(string key, string fallback = "") => Active != null ? Active.entries.Find(e => e.key == key)?.value ?? fallback : PlayerPrefs.GetString(key, fallback);
    public static int GetInt(string key, int fallback = 0) => Active != null ? (int.TryParse(GetString(key), out int n) ? n : fallback) : PlayerPrefs.GetInt(key, fallback);
    public static void SetString(string key, string value)
    {
        if (Active == null) { PlayerPrefs.SetString(key, value); return; }
        var entry = Active.entries.Find(e => e.key == key);
        if (entry == null) Active.entries.Add(new Entry { key = key, value = value });
        else entry.value = value;
    }
    public static void SetInt(string key, int value) { if (Active == null) PlayerPrefs.SetInt(key, value); else SetString(key, value.ToString()); }
    public static void DeleteKey(string key) { if (Active == null) PlayerPrefs.DeleteKey(key); else Active.entries.RemoveAll(e => e.key == key); }
    public static void Save() { if (Active == null) PlayerPrefs.Save(); }
    public static void Report(Exception e) { LastError = e.Message; Debug.LogError("저장 실패: " + e); }
}
