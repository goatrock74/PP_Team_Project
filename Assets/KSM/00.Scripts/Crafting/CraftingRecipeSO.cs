using System;
using System.Text;
using UnityEngine;
using KSM._00.Scripts.Items;
 
namespace KSM._00.Scripts.Crafting
{
    [Serializable]
    public struct MaterialCost
    {
        public ItemSO item;
 
        [Tooltip("1회 제작에 필요한 개수")]
        [Min(1)] public int count;
    }
    [CreateAssetMenu(fileName = "CraftingRecipeSO", menuName = "SO/Crafting/Recipe")]
    public class CraftingRecipeSO : ScriptableObject
    {
        [Header("결과물")]
        public ItemSO result;
 
        [Tooltip("1회 제작으로 나오는 개수")]
        [Min(1)] public int resultCount = 1;
 
        [Header("재료")]
        public MaterialCost[] materials;
 
        [Header("제작")]
        [Tooltip("한 번에 만들 수 있는 최대 횟수. 도구는 1, 씨앗·소모품은 10 처럼")]
        [Range(1, 10)] public int maxBatch = 1;
 
        [Tooltip("상세창의 능력치 설명. 비우면 결과 아이템의 Description 을 쓴다")]
        [TextArea(2, 4)] public string description;
 
        public string DisplayName => result != null ? result.DisplayName : name;
 
        public Sprite Icon => result != null ? result.icon : null;
 
        public string DescriptionText => string.IsNullOrWhiteSpace(description)
            ? (result != null ? result.description : string.Empty)
            : description;
        public bool HasMaterials(PlayerInventory player, int batch)
        {
            if (player == null || materials == null) return false;
 
            batch = Mathf.Max(1, batch);
 
            foreach (MaterialCost m in materials)
            {
                if (m.item == null) continue;
                if (player.CountOf(m.item) < m.count * batch) return false;
            }
 
            return true;
        }
        public int MaxAffordable(PlayerInventory player)
        {
            if (player == null || result == null) return 0;
            if (materials == null || materials.Length == 0) return maxBatch;
 
            int best = maxBatch;
 
            foreach (MaterialCost m in materials)
            {
                if (m.item == null || m.count <= 0) continue;
 
                int possible = player.CountOf(m.item) / m.count;
                if (possible < best) best = possible;
 
                if (best <= 0) return 0;
            }
 
            return Mathf.Clamp(best, 0, maxBatch);
        }
        public string BuildMaterialText(PlayerInventory player, int batch)
        {
            if (materials == null || materials.Length == 0) return "<color=#AAAAAA>재료 없음</color>";
 
            batch = Mathf.Max(1, batch);
 
            var sb = new StringBuilder();
            bool first = true;
 
            foreach (MaterialCost m in materials)
            {
                if (m.item == null) continue;
 
                int need = m.count * batch;
                int have = player != null ? player.CountOf(m.item) : 0;
                bool enough = have >= need;
 
                if (!first) sb.Append("   ·   ");
                first = false;
 
                // 가진 개수가 필요량을 넘어도 need 로 잘라서 보여준다 (3/3 이 5/3 보다 읽기 쉽다)
                sb.Append($"<color=#{(enough ? "6FCF6F" : "FF6B6B")}>" +
                          $"{m.item.DisplayName} {Mathf.Min(have, need)}/{need}개</color>");
            }
 
            return sb.Length == 0 ? "<color=#AAAAAA>재료 없음</color>" : sb.ToString();
        }
        public bool TryCraft(PlayerInventory player, int batch, out string failReason)
        {
            failReason = string.Empty;
 
            if (player == null || result == null)
            {
                failReason = "레시피의 결과 아이템이 비어있습니다";
                return false;
            }
 
            batch = Mathf.Clamp(batch, 1, Mathf.Max(1, maxBatch));
 
            if (!HasMaterials(player, batch))
            {
                failReason = "재료가 부족합니다";
                return false;
            }
 
            int total = resultCount * batch;
 
            if (!player.CanAccept(result, total))
            {
                failReason = "가방에 자리가 없습니다";
                return false;
            }
 
            foreach (MaterialCost m in materials)
            {
                if (m.item == null) continue;
                player.Remove(m.item, m.count * batch);
            }
 
            player.Add(result, total);
            return true;
        }
 
        private void OnValidate()
        {
            resultCount = Mathf.Max(1, resultCount);
            maxBatch = Mathf.Clamp(maxBatch, 1, 10);
 
            if (materials == null) return;
            for (int i = 0; i < materials.Length; i++)
            {
                MaterialCost m = materials[i];
                if (m.count <= 0) m.count = 1;
 
                materials[i] = m;
            }
        }
    }
}