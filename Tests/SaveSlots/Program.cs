using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

// Storage tests use a temporary directory and a JsonUtility-compatible fields-only shim.
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) {} }
    public struct Vector3 { public float x, y, z; }
    public static class Application { public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "FarmSaveChecks-" + Guid.NewGuid()); }
    public static class Debug { public static void LogWarning(string s) => Console.WriteLine(s); public static void LogError(string s) => Console.WriteLine(s); }
    public static class JsonUtility
    {
        static JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson<T>(T data, bool pretty = false) => JsonSerializer.Serialize(data, options);
        public static T FromJson<T>(string json) { try { return JsonSerializer.Deserialize<T>(json, options); } catch (JsonException e) { throw new ArgumentException("Invalid JSON", e); } }
    }
    public static class PlayerPrefs
    {
        static Dictionary<string,string> values = new Dictionary<string,string>();
        public static bool HasKey(string key) => values.ContainsKey(key);
        public static string GetString(string key, string fallback = "") => values.TryGetValue(key, out var v) ? v : fallback;
        public static int GetInt(string key, int fallback = 0) => int.TryParse(GetString(key), out int v) ? v : fallback;
        public static void SetString(string key, string value) => values[key] = value;
        public static void SetInt(string key, int value) => SetString(key, value.ToString());
        public static void DeleteKey(string key) => values.Remove(key);
        public static void Save() {}
    }
}
class Program
{
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static void Main()
    {
        var a = SaveSlotStore.Create(); var b = SaveSlotStore.Create();
        Check(a.id != b.id && SaveSlotStore.List().Count == 2, "NEW creates distinct persisted rows");
        UnityEngine.PlayerPrefs.SetInt("TotalMoney", 999999);
        SaveSlotStore.Select(a.id);
        Check(SaveSlotStore.Active.money == 0 && SaveSlotStore.GetInt("TotalMoney", 0) == 0, "new slot starts at zero and ignores legacy balance");
        SaveSlotStore.SetString("Inventory", "seed:7"); SaveSlotStore.SetInt("OncePurchase", 1);
        SaveSlotStore.Active.day = 17; SaveSlotStore.Active.money = 3512;
        SaveSlotStore.Write(SaveSlotStore.Active);
        SaveSlotStore.Select(b.id);
        Check(!SaveSlotStore.HasKey("Inventory") && SaveSlotStore.GetInt("OncePurchase") == 0, "inventory and purchase isolation");
        SaveSlotStore.Select(a.id);
        Check(SaveSlotStore.GetString("Inventory") == "seed:7" && SaveSlotStore.Active.day == 17 && SaveSlotStore.Active.money == 3512, "snapshot round trip");
        SaveSlotStore.Active.day = 18; SaveSlotStore.Write(SaveSlotStore.Active);
        File.WriteAllText(Path.Combine(SaveSlotStore.DirectoryPath, a.id + ".json"), "broken");
        Check(SaveSlotStore.Read(a.id).day == 17, "corrupt primary recovers previous backup");
        SaveSlotStore.Deselect(); SaveSlotStore.Delete(a.id);
        Check(!File.Exists(Path.Combine(SaveSlotStore.DirectoryPath, a.id + ".json.bak")) && SaveSlotStore.List().Count == 1, "delete removes primary and backup only for target slot");
        Check(SaveSlotStore.Read(b.id).day == 1, "other slot survives deletion");
        bool rejected = false; try { SaveSlotStore.Read("../escape"); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "invalid slot paths rejected");
        var orphan = SaveSlotStore.Create(); SaveSlotStore.Write(orphan);
        File.Delete(Path.Combine(SaveSlotStore.DirectoryPath, orphan.id + ".json"));
        Check(SaveSlotStore.List().Exists(s => s.id == orphan.id), "backup-only slot remains visible");
        var recovered = SaveSlotStore.Create(); recovered.day = 7; SaveSlotStore.Write(recovered);
        string recoveryPath = Path.Combine(SaveSlotStore.DirectoryPath, recovered.id + ".json");
        File.WriteAllText(recoveryPath, "broken");
        recovered = SaveSlotStore.Read(recovered.id); SaveSlotStore.Write(recovered);
        File.WriteAllText(recoveryPath, "broken again");
        Check(SaveSlotStore.Read(recovered.id).day == 1, "saving recovered slot preserves valid backup");
        var malformed = SaveSlotStore.Create();
        string malformedPath = Path.Combine(SaveSlotStore.DirectoryPath, malformed.id + ".json");
        malformed.entries.Add(null);
        File.WriteAllText(malformedPath, UnityEngine.JsonUtility.ToJson(malformed));
        Check(SaveSlotStore.List().Find(s => s.id == malformed.id).unavailable, "null entries rejected before gameplay");
        Directory.Delete(UnityEngine.Application.persistentDataPath, true);
    }
}
