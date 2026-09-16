using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점의 <b>Item</b>(PJW) 과 인벤토리의 <b>ItemSO</b>(KSM) 를 이어주는 표.
///
/// 두 시스템이 서로를 모르는 채로 만들어졌기 때문에, 어느 쪽 코드도 고치지 않고
/// 여기서만 짝을 지어준다. 나중에 한쪽 구조가 바뀌어도 이 파일만 손보면 된다.
///
/// 만드는 법 : Project 창 우클릭 → Create → SO → Shop → Item Map
/// 채우는 법 : Pairs 를 손으로 채워도 되고, 아래 <b>이름으로 자동 연결</b> 을 쓰면
///             이름이 같은 것끼리 한 번에 붙는다.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemMapSO", menuName = "SO/Shop/Item Map")]
public class ShopItemMapSO : ScriptableObject
{
    [Serializable]
    public struct Pair
    {
        [Tooltip("상점 쪽 아이템 (PJW)")]
        public Item shopItem;

        [Tooltip("인벤토리 쪽 아이템 (KSM)")]
        public ItemSO inventoryItem;
    }

    [Tooltip("상점 아이템 ↔ 인벤토리 아이템 짝")]
    public Pair[] pairs;

    private Dictionary<Item, ItemSO> _toInventory;
    private Dictionary<ItemSO, Item> _toShop;

    // ════════════════════════════════════════════════════════════

    /// <summary>상점 아이템 → 인벤토리 아이템. 짝이 없으면 null</summary>
    public ItemSO ToInventory(Item shopItem)
    {
        if (shopItem == null) return null;

        Build();
        return _toInventory.TryGetValue(shopItem, out ItemSO so) ? so : null;
    }

    /// <summary>인벤토리 아이템 → 상점 아이템. 짝이 없으면 null</summary>
    public Item ToShop(ItemSO inventoryItem)
    {
        if (inventoryItem == null) return null;

        Build();
        return _toShop.TryGetValue(inventoryItem, out Item item) ? item : null;
    }

    /// <summary>표에 등록된 모든 상점 아이템</summary>
    public IEnumerable<Item> ShopItems
    {
        get
        {
            Build();
            return _toInventory.Keys;
        }
    }

    private void Build()
    {
        if (_toInventory != null) return;

        _toInventory = new Dictionary<Item, ItemSO>();
        _toShop = new Dictionary<ItemSO, Item>();

        if (pairs == null) return;

        foreach (Pair p in pairs)
        {
            if (p.shopItem == null || p.inventoryItem == null) continue;

            _toInventory[p.shopItem] = p.inventoryItem;
            _toShop[p.inventoryItem] = p.shopItem;   // 같은 ItemSO 가 겹치면 마지막 것이 남는다
        }
    }

    /// <summary>에셋을 고칠 때마다 표를 다시 만든다</summary>
    private void OnValidate() => _toInventory = null;

    // ════════════════════════════════════════════════════════════
    //  편의 기능
    // ════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("비어 있는 칸 검사")]
    private void Validate()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ {name} 검사 ═══");

        int bad = 0;
        var seenShop = new HashSet<Item>();
        var seenInv = new HashSet<ItemSO>();

        for (int i = 0; i < (pairs?.Length ?? 0); i++)
        {
            Pair p = pairs[i];

            if (p.shopItem == null || p.inventoryItem == null)
            {
                sb.AppendLine($"  [{i}] ★ 한쪽이 비어 있습니다");
                bad++;
                continue;
            }

            if (!seenShop.Add(p.shopItem))
            {
                sb.AppendLine($"  [{i}] ★ 상점 아이템 '{p.shopItem.name}' 이 중복입니다");
                bad++;
            }

            if (!seenInv.Add(p.inventoryItem))
            {
                sb.AppendLine($"  [{i}] ★ 인벤토리 아이템 '{p.inventoryItem.name}' 이 중복입니다");
                bad++;
            }
        }

        sb.AppendLine(bad == 0 ? $"→ 이상 없음 ({seenShop.Count}쌍)" : $"→ ★ {bad}곳 확인 필요");

        if (bad > 0) Debug.LogError(sb.ToString(), this);
        else Debug.Log(sb.ToString(), this);
    }

    [ContextMenu("이름으로 자동 연결")]
    private void AutoLink()
    {
        // 프로젝트의 모든 ItemSO 를 이름으로 모아둔다
        var byName = new Dictionary<string, ItemSO>();

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ItemSO"))
        {
            var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

            if (so == null) continue;

            byName[Key(so.DisplayName)] = so;
            byName[Key(so.name)] = so;
        }

        int linked = 0;
        int missed = 0;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ {name} 자동 연결 ═══");

        for (int i = 0; i < (pairs?.Length ?? 0); i++)
        {
            Pair p = pairs[i];

            if (p.shopItem == null || p.inventoryItem != null) continue;

            if (byName.TryGetValue(Key(p.shopItem.Item_name), out ItemSO hit) ||
                byName.TryGetValue(Key(p.shopItem.name), out hit))
            {
                p.inventoryItem = hit;
                pairs[i] = p;
                linked++;

                sb.AppendLine($"  {p.shopItem.name} → {hit.name}");
            }
            else
            {
                missed++;
                sb.AppendLine($"  ★ {p.shopItem.name} : 이름이 같은 ItemSO 를 못 찾았습니다");
            }
        }

        sb.AppendLine($"→ 연결 {linked}개 / 실패 {missed}개");
        Debug.Log(sb.ToString(), this);

        _toInventory = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>띄어쓰기·대소문자 차이는 무시하고 맞춰본다</summary>
    private static string Key(string s)
        => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Replace(" ", string.Empty).ToLowerInvariant();
#endif
}
