using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.PJW.Script.SO_Script;
using KSM._00.Scripts.Items;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ShopValidation
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static ShopValidation()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Temp/ShopValidation.request")) return;
            File.Delete("Temp/ShopValidation.request");
            Run();
        };
    }
    [MenuItem("Tools/Shop/SO 연결·저장 형식 자동 검증")]
    public static void Run()
    {
        var log = new StringBuilder();
        string testKey = "ShopValidation_" + Guid.NewGuid().ToString("N");
        var root = new GameObject("Shop Validation (temporary)");
        root.SetActive(false);
        try
        {
            InventoryCatalogBuilder.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<InventoryItemCatalog>(InventoryCatalogBuilder.Path);
            Check(catalog != null && catalog.items.Length > 0, "저장 카탈로그 존재");
            Check(catalog.items.Select(entry => entry.id).Distinct().Count() == catalog.items.Length, "에셋 ID 중복 없음");
            var shop = root.AddComponent<Input_SO_Data>();
            var lists = AssetDatabase.FindAssets("t:ItemListSO", new[] { "Assets/PJW/Data/SO-Item/Item_List_SO" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemListSO>(AssetDatabase.GUIDToAssetPath(guid))).Reverse().ToArray();
            var buttons = new List<Plant_Shop_BT>();
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Slot " + i);
                go.transform.SetParent(root.transform);
                buttons.Add(go.AddComponent<Plant_Shop_BT>());
            }
            Set(shop, "shopButtons", buttons);
            shop.SetItemListSO(lists, false);
            SeasonType[] expected = { SeasonType.Spring, SeasonType.Summer, SeasonType.Autumn, SeasonType.Winter };
            for (int season = 0; season < 4; season++)
            {
                ItemListSO list = shop.GetSeasonList(season);
                Check(list != null && list.TargetSeason == expected[season], "배열 순서와 무관한 계절 SO 선택");
                shop.UpdateShopBySeason((TimeManager.SeasonPeriod)season);
                for (int i = 0; i < buttons.Count; i++)
                    Check((Item)Get(buttons[i], "currentItem") == (i < list.ItemList.Length ? list.ItemList[i] : null), "실제 버튼 상품 갱신");
                log.AppendLine($"PASS {expected[season]}: {list.name}, {list.ItemList.Length}개 상품 버튼 갱신");
            }
            var save = root.AddComponent<Inventorysavemanger>();
            var dictionary = (Dictionary<string, ItemSO>)Get(save, "items");
            var keys = (Dictionary<ItemSO, string>)Get(save, "itemKeys");
            foreach (var entry in catalog.items) { dictionary.Add(entry.id, entry.item); keys.Add(entry.item, entry.id); }
            ItemSO sample = catalog.items.First(entry => entry.item.maxStack >= 3).item;
            var bag = new Inventory(4);
            var hotbar = new Inventory(2);
            bag.Add(sample, 3, ItemQuality.Good);
            hotbar.Add(sample, 1, ItemQuality.Best);
            Func<Inventory, Inventorysavemanger.SlotData[]> capture = inventory =>
                (Inventorysavemanger.SlotData[])Call(save, "Capture", inventory);
            var data = new Inventorysavemanger.SaveData { bag = capture(bag), hotbar = capture(hotbar) };
            var loaded = JsonUtility.FromJson<Inventorysavemanger.SaveData>(JsonUtility.ToJson(data));
            var restoredBag = (ItemStack[])Call(save, "Decode", loaded.bag, 4);
            var restoredHotbar = (ItemStack[])Call(save, "Decode", loaded.hotbar, 2);
            Check(restoredBag[0].item == sample && restoredBag[0].count == 3 && restoredBag[0].quality == ItemQuality.Good, "가방 복원");
            Check(restoredBag[1] == null && restoredHotbar[0].quality == ItemQuality.Best, "빈 슬롯·핫바·품질 복원");
            bag.Remove(sample, 3);
            var empty = (ItemStack[])Call(save, "Decode", capture(bag), 4);
            Check(empty.All(slot => slot == null), "전량 판매 후 빈 인벤토리 복원");
            bool rejected = false;
            loaded.bag[0].itemId = "__missing_test_item__";
            try { Call(save, "Decode", loaded.bag, 4); }
            catch (TargetInvocationException) { rejected = true; }
            Check(rejected, "알 수 없는 아이템 데이터 거부");
            log.AppendLine("PASS 가방/핫바 JSON 왕복, 수량·품질·빈 슬롯, 전량 판매, 누락 ItemId 보호");
            Set(save, "storageKey", testKey);
            Set(save, "bag", bag);
            Set(save, "hotbar", hotbar);
            save.LoadInventory();
            bag.Add(sample, 2, ItemQuality.Normal);
            save.SaveInventory();
            Check(PlayerPrefs.HasKey(testKey), "실제 저장소 기록");
            bag.Clear();
            hotbar.Clear();
            save.LoadInventory();
            Check(bag.CountOf(sample) == 2 && hotbar.CountOf(sample, ItemQuality.Best) == 1, "실제 저장소에서 복원");
            bag.Remove(sample, 2);
            save.SaveInventory();
            save.LoadInventory();
            Check(bag.CountOf(sample) == 0, "판매 후 저장·재로드 시 수량 유지");
            log.AppendLine("PASS 분리된 PlayerPrefs 키로 실제 저장·복원·판매 후 재로드");
            log.AppendLine($"PASS 저장 카탈로그 {catalog.items.Length}종 / 사용자 저장 데이터 변경 없음");
            Debug.Log(log.ToString());
        }
        catch (Exception exception)
        {
            log.AppendLine("FAIL " + exception);
            Debug.LogError(log.ToString());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            PlayerPrefs.DeleteKey(testKey);
            PlayerPrefs.Save();
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/ShopValidation.result.txt", log.ToString());
        }
    }
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Get(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
}
