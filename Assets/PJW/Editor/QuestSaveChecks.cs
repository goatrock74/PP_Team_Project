using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Exercises the actual quest serializer without loading gameplay or touching existing slots.
public static class QuestSaveChecks
{
    private const string Key = "QuestBoardV1";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Save System/Run Quest Slot Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SaveSlotStore.Active != null)
            throw new InvalidOperationException("Stop gameplay before running quest save checks.");

        var slots = new List<string>();
        var objects = new List<GameObject>();
        var results = new List<string>();
        string legacy = PlayerPrefs.GetString(Key, "");
        try
        {
            string a = SaveSlotStore.Create().id; slots.Add(a);
            string b = SaveSlotStore.Create().id; slots.Add(b);
            SaveSlotStore.Select(a);
            var boardA = Board(objects);
            Check(Progress(boardA).Count == 0 && Claimed(boardA).Count == 0,
                "New slot starts with empty quest progress", results);
            Call(boardA, "RerollDaily", 7);
            Progress(boardA)["story-check"] = 3;
            Claimed(boardA).Add("claimed-check");
            Progress(boardA)["daily#0"] = 2;
            Claimed(boardA).Add("daily#0");
            Call(boardA, "MarkDirty");
            boardA.CaptureSave(); // Capture immediately, before the one-second delayed save.
            SaveSlotStore.Write(SaveSlotStore.Active);
            string saved = SaveSlotStore.GetString(Key);

            SaveSlotStore.Select(b);
            var boardB = Board(objects);
            Check(Progress(boardB).Count == 0 && Claimed(boardB).Count == 0,
                "Second slot does not inherit story or daily progress", results);
            boardA.CaptureSave();
            Check(!SaveSlotStore.HasKey(Key), "Old board cannot write into another slot", results);
            Progress(boardB)["second-slot"] = 1;
            boardB.CaptureSave();
            SaveSlotStore.Write(SaveSlotStore.Active);

            SaveSlotStore.Select(a);
            var restored = Board(objects);
            Check(Progress(restored)["story-check"] == 3 && Claimed(restored).Contains("claimed-check"),
                "Story counts and claimed rewards survive reload", results);
            Check(Progress(restored)["daily#0"] == 2 && Claimed(restored).Contains("daily#0")
                && (int)Field("_dailyDay").GetValue(restored) == 7,
                "Daily day, progress and claimed rewards survive reload", results);
            restored.CaptureSave();
            Check(SaveSlotStore.GetString(Key) == saved, "Reload preserves daily templates and target counts", results);

            Call(restored, "ResetProgress");
            SaveSlotStore.Write(SaveSlotStore.Active);
            SaveSlotStore.Select(a);
            var reset = Board(objects);
            Check(Progress(reset).Count == 0 && Claimed(reset).Count == 0,
                "Reset remains empty after reloading the slot", results);
            SaveSlotStore.Select(b);
            var other = Board(objects);
            Check(Progress(other)["second-slot"] == 1, "Reset leaves the other slot intact", results);

            SaveSlotStore.Deselect();
            boardA.CaptureSave();
            Check(PlayerPrefs.GetString(Key, "") == legacy, "Old board cannot overwrite legacy quests after menu return", results);
        }
        catch (Exception e)
        {
            results.Add("FAIL: " + e);
            throw;
        }
        finally
        {
            SaveSlotStore.Deselect();
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            foreach (var id in slots) SaveSlotStore.Delete(id);
            Directory.CreateDirectory("Temp");
            File.WriteAllLines("Temp/QuestSaveChecks.txt", results);
        }
        Debug.Log(string.Join("\n", results));
    }

    private static QuestBoard Board(List<GameObject> objects)
    {
        var go = new GameObject("Quest Save Check");
        go.SetActive(false); // Test persistence only; do not build UI or subscribe to gameplay.
        objects.Add(go);
        var board = go.AddComponent<QuestBoard>();
        Call(board, "BuildIds");
        Call(board, "Load");
        return board;
    }
    private static FieldInfo Field(string name) => typeof(QuestBoard).GetField(name, Private);
    private static Dictionary<string, int> Progress(QuestBoard board) => (Dictionary<string, int>)Field("_progress").GetValue(board);
    private static HashSet<string> Claimed(QuestBoard board) => (HashSet<string>)Field("_claimed").GetValue(board);
    private static void Call(QuestBoard board, string method, params object[] args) => typeof(QuestBoard).GetMethod(method, Private).Invoke(board, args);
    private static void Check(bool condition, string message, List<string> results)
    {
        if (!condition) throw new Exception(message);
        results.Add("PASS: " + message);
    }
}
