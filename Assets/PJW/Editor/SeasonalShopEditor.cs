using System;
using System.Linq;
using Assets.PJW.Script.SO_Script;
using KSM._00.Scripts.Items;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[CustomEditor(typeof(Input_SO_Data))]
public class SeasonalShopEditor : Editor
{
    private static readonly string[] Seasons = { "봄", "여름", "가을", "겨울" };
    private int preview;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var shop = (Input_SO_Data)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("계절별 SO 확인", EditorStyles.boldLabel);
        preview = GUILayout.Toolbar(preview, Seasons);
        ItemListSO list = shop.GetSeasonList(preview);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("선택될 SO", list, typeof(ItemListSO), false);
        if (list == null) EditorGUILayout.HelpBox("이 계절에 연결된 SO가 없습니다.", MessageType.Error);
        else
        {
            var serializedList = new SerializedObject(list);
            EditorGUILayout.PropertyField(serializedList.FindProperty("<ItemList>k__BackingField"), new GUIContent("상품 목록"), true);
            serializedList.ApplyModifiedProperties();
            int buttons = serializedObject.FindProperty("shopButtons").arraySize;
            if ((list.ItemList?.Length ?? 0) > buttons)
                EditorGUILayout.HelpBox($"상품 {list.ItemList.Length}개 / 버튼 {buttons}개: 초과 상품은 표시되지 않습니다.", MessageType.Warning);
        }
        var lists = serializedObject.FindProperty("itemListSO");
        var seen = new System.Collections.Generic.HashSet<SeasonType>();
        for (int i = 0; i < lists.arraySize; i++)
        {
            var entry = lists.GetArrayElementAtIndex(i).objectReferenceValue as ItemListSO;
            if (entry != null && !seen.Add(entry.TargetSeason))
                EditorGUILayout.HelpBox($"계절 중복: {entry.TargetSeason}. 첫 번째 SO만 사용합니다.", MessageType.Error);
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("위 탭은 SO 미리보기입니다. 플레이 중 아래 버튼으로 실제 상점 목록을 바꿀 수 있습니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("선택한 계절을 상점에 적용")) shop.UpdateShopBySeason((TimeManager.SeasonPeriod)preview);
        }
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("현재 표시 계절", Seasons[shop.CurrentSeasonIndex]);
            Inventorysavemanger save = Inventorysavemanger.Instance;
            EditorGUILayout.LabelField("인벤토리 저장", save != null ? save.Status : "초기화 대기");
            PlayerInventory player = PlayerInventory.Instance;
            if (player != null && player.Bag != null)
            {
                DrawInventory("가방", player.Bag);
                DrawInventory("핫바", player.Hotbar);
            }
            Repaint();
        }
        if (GUILayout.Button("SO 연결·저장 형식 자동 검증")) ShopValidation.Run();
    }
    private static void DrawInventory(string title, Inventory inventory)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        for (int i = 0; i < inventory.Capacity; i++)
        {
            ItemStack slot = inventory.GetSlot(i);
            if (slot != null) EditorGUILayout.LabelField($"{i + 1}: {slot.item.DisplayName}", $"{slot.count}개 / {slot.quality}");
        }
    }
}

[CustomEditor(typeof(Inventorysavemanger))]
public class InventorySaveInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var save = (Inventorysavemanger)target;
        EditorGUILayout.HelpBox(save.Status, MessageType.Info);
        EditorGUILayout.LabelField("저장 대상", "에셋 ID · 수량 · 품질 · 가방/핫바 슬롯");
        EditorGUILayout.LabelField("저장 키", Inventorysavemanger.SaveKey);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !save.IsReady))
            if (GUILayout.Button("지금 저장")) save.SaveInventory();
        if (Application.isPlaying) Repaint();
    }
}

[InitializeOnLoad]
public class InventoryCatalogBuilder : IPreprocessBuildWithReport
{
    public const string Path = "Assets/Resources/InventoryItemCatalog.asset";
    public int callbackOrder => 0;
    static InventoryCatalogBuilder() { EditorApplication.delayCall += Refresh; }
    [MenuItem("Tools/Shop/저장용 아이템 카탈로그 갱신")]
    public static void Refresh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var items = AssetDatabase.FindAssets("t:ItemSO").OrderBy(guid => guid)
            .Select(guid => new InventoryItemCatalog.Entry { id = guid,
                item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid)) })
            .Where(entry => entry.item != null).ToArray();
        InventoryItemCatalog catalog = AssetDatabase.LoadAssetAtPath<InventoryItemCatalog>(Path);
        if (catalog == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            catalog = ScriptableObject.CreateInstance<InventoryItemCatalog>();
            AssetDatabase.CreateAsset(catalog, Path);
        }
        if (catalog.items != null && catalog.items.Length == items.Length
            && catalog.items.Zip(items, (a, b) => a != null && a.id == b.id && a.item == b.item).All(equal => equal)) return;
        catalog.items = items;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
    }
    public void OnPreprocessBuild(BuildReport report)
    {
        Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<InventoryItemCatalog>(Path);
        var duplicate = catalog.items.GroupBy(entry => entry.id).FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null) throw new BuildFailedException("중복 인벤토리 에셋 ID: " + duplicate.Key);
    }
}

public class InventoryCatalogAssetWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Any(path => path.EndsWith(".asset") && path != InventoryCatalogBuilder.Path))
            EditorApplication.delayCall += InventoryCatalogBuilder.Refresh;
    }
}
