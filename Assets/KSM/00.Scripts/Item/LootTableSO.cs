using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
 
/// <summary>
/// 확률표 한 줄. 씨앗에 한정되지 않고 아무 ItemSO 나 넣을 수 있다.
/// </summary>
[Serializable]
public struct LootEntry
{
    [Tooltip("뽑힐 아이템. 씨앗이든 열매든 도구든 상관없다")]
    public ItemSO item;
 
    [Tooltip("가중치. 클수록 잘 나온다. 전체 합 대비 비율이 곧 확률")]
    [Min(1)] public int weight;
 
    [Min(1)] public int minCount;
    [Min(1)] public int maxCount;
 
    [Tooltip("뽑혔을 때 붙을 품질")]
    public ItemQuality quality;
 
    [Tooltip("연출용 등급 라벨. 확률과는 무관하게 색/이름만 정한다")]
    public ItemRarity rarity;
 
    public int RollCount() => UnityEngine.Random.Range(Mathf.Max(1, minCount), Mathf.Max(minCount, maxCount) + 1);
 
    public bool IsValid => item != null && weight > 0;
}
 
/// <summary>
/// 가중치 기반 확률표. 씨드팩·상자·낚시 결과 등 어디에나 재사용할 수 있다.
///
/// 확률은 weight 의 비율로 자동 계산된다. 예를 들어 weight 가 90/8/2 면
/// 각각 90% / 8% / 2% 다. 합이 100 이 아니어도 되니 밸런스를 고칠 때
/// 한 줄만 바꾸면 나머지가 알아서 재계산된다.
/// </summary>
[CreateAssetMenu(fileName = "LootTableSO", menuName = "SO/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [Tooltip("이 표의 이름 (정보창에 표시)")]
    public string tableName;
 
    public LootEntry[] entries;
 
    /// <summary>유효한 항목들의 가중치 합</summary>
    public int TotalWeight
    {
        get
        {
            int sum = 0;
            if (entries == null) return 0;
 
            foreach (LootEntry e in entries)
                if (e.IsValid) sum += e.weight;
 
            return sum;
        }
    }
 
    public bool IsUsable => TotalWeight > 0;
 
    /// <summary>가중치에 따라 한 줄을 뽑는다</summary>
    public LootEntry Roll()
    {
        int total = TotalWeight;
        if (total <= 0) return default;
 
        int pick = UnityEngine.Random.Range(0, total);
 
        foreach (LootEntry e in entries)
        {
            if (!e.IsValid) continue;
 
            pick -= e.weight;
            if (pick < 0) return e;
        }
 
        // 부동소수 문제 없는 정수 연산이라 여기 올 일은 없지만 방어
        return LastValid();
    }
 
    /// <summary>연출용으로 여러 개 뽑는다 (룰렛 띠를 채울 때)</summary>
    public void RollMany(List<LootEntry> buffer, int count)
    {
        buffer.Clear();
        if (!IsUsable) return;
 
        for (int i = 0; i < count; i++) buffer.Add(Roll());
    }
 
    public float ChanceOf(in LootEntry entry)
    {
        int total = TotalWeight;
        return total <= 0 ? 0f : (float)entry.weight / total;
    }
 
    /// <summary>
    /// 정보창에 그대로 넣을 확률표 문자열. 희귀한 것부터 위에 온다.
    /// </summary>
    public string BuildChanceText()
    {
        if (!IsUsable) return "<color=#FF7777>확률표가 비어있습니다</color>";
 
        // 원본 배열을 건드리지 않도록 복사해서 정렬
        var list = new List<LootEntry>();
        foreach (LootEntry e in entries)
            if (e.IsValid) list.Add(e);
 
        list.Sort((a, b) => a.weight.CompareTo(b.weight));   // 낮은 확률이 위로
 
        var sb = new StringBuilder();
        int total = TotalWeight;
 
        foreach (LootEntry e in list)
        {
            float pct = 100f * e.weight / total;
            string hex = ItemRarityUtil.ColorHex(e.rarity);
            string count = e.maxCount > e.minCount ? $" x{e.minCount}~{e.maxCount}"
                         : e.minCount > 1 ? $" x{e.minCount}"
                         : string.Empty;
 
            sb.Append($"\n<color=#{hex}>{ItemRarityUtil.DisplayName(e.rarity)}</color>  " +
                      $"{e.item.DisplayName}{count}  <b>{pct:0.#}%</b>");
        }
 
        return sb.ToString();
    }
 
    private LootEntry LastValid()
    {
        for (int i = entries.Length - 1; i >= 0; i--)
            if (entries[i].IsValid) return entries[i];
 
        return default;
    }
 
    private void OnValidate()
    {
        if (entries == null) return;
 
        for (int i = 0; i < entries.Length; i++)
        {
            LootEntry e = entries[i];
 
            e.weight = Mathf.Max(1, e.weight);
            e.minCount = Mathf.Max(1, e.minCount);
            e.maxCount = Mathf.Max(e.minCount, e.maxCount);
 
            entries[i] = e;
        }
    }
}
 