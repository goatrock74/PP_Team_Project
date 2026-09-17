using System;
using System.Text;
using UnityEngine;
using KSM._00.Scripts.Items;

namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 재료 한 줄. 품질은 가리지 않는다 — '좋음' 딸기도 재료로 쓸 수 있다.
    /// </summary>
    /// <remarks>구조체라 필드 초기화식을 못 쓴다. 기본값은 CraftingRecipeSO.OnValidate 에서 넣는다.</remarks>
    [Serializable]
    public struct MaterialCost
    {
        public ItemSO item;

        [Tooltip("1회 제작에 필요한 개수")]
        [Min(1)] public int count;
    }

    /// <summary>
    /// 제작법 하나. "무엇을 넣으면 무엇이 나오는가" 만 담는다.
    ///
    /// ★ 여기에는 진행 상황을 저장하지 않는다. SO 는 읽기 전용 데이터고,
    ///   재료가 몇 개 있는지는 전부 PlayerInventory 가 들고 있다.
    /// </summary>
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

        // ════════════════════════════════════════════════════════════
        //  판정
        // ════════════════════════════════════════════════════════════

        /// <summary>이 횟수만큼 만들 재료가 있는가</summary>
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

        /// <summary>
        /// 지금 재료로 몇 번까지 만들 수 있는가 (Max Batch 로 잘린다).
        /// 재료가 아예 없으면 0.
        /// </summary>
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

        /// <summary>
        /// 상세창에 넣을 재료 문자열. 충분하면 초록, 모자라면 빨강.
        /// 재료 하나당 한 줄씩 나간다.
        /// <code>
        /// · 딸기 3/3개
        /// · 키위 1/3개
        /// </code>
        /// </summary>
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

                // 재료 하나당 한 줄. 마지막 줄 뒤에는 빈 줄이 생기지 않게 앞에 붙인다
                if (!first) sb.Append('\n');
                first = false;

                // 가진 개수가 필요량을 넘어도 need 로 잘라서 보여준다 (3/3 이 5/3 보다 읽기 쉽다)
                sb.Append($"<color=#{(enough ? "6FCF6F" : "FF6B6B")}>" +
                          $"{bulletPrefix}{m.item.DisplayName} {Mathf.Min(have, need)}/{need}개</color>");
            }

            return sb.Length == 0 ? "<color=#AAAAAA>재료 없음</color>" : sb.ToString();
        }

        /// <summary>줄 앞에 붙는 글머리표. 비우면 안 붙는다</summary>
        private const string bulletPrefix = "· ";

        // ════════════════════════════════════════════════════════════
        //  실행
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 재료를 먼저 빼고, 결과물은 나중에 준다 (1단계).
        ///
        /// ★ 재료를 <b>지금</b> 빼는 이유 — 연출이 도는 동안 제작 버튼을 또 누르거나
        ///   창을 닫아도 같은 재료로 두 번 만들어지면 안 된다.
        /// ★ 결과물을 <b>나중에</b> 주는 이유 — "제작중" 연출이 끝나는 순간 아이템이
        ///   들어와야 자연스럽다. 시작하자마자 들어오면 연출이 겉돈다.
        ///
        /// 성공하면 반드시 <see cref="Deliver"/> 를 불러줘야 한다.
        /// </summary>
        public bool TryReserve(PlayerInventory player, int batch, out string failReason)
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

            // ★ 자리 확인은 재료를 빼기 전에.
            //   안 그러면 가방이 꽉 찼을 때 재료만 사라진다
            if (!player.CanAccept(result, resultCount * batch))
            {
                failReason = "가방에 자리가 없습니다";
                return false;
            }

            foreach (MaterialCost m in materials)
            {
                if (m.item == null) continue;
                player.Remove(m.item, m.count * batch);
            }

            return true;
        }

        /// <summary>결과물을 넣는다 (2단계). 연출이 끝난 뒤에 부른다</summary>
        public void Deliver(PlayerInventory player, int batch)
        {
            if (player == null || result == null) return;

            batch = Mathf.Clamp(batch, 1, Mathf.Max(1, maxBatch));

            int total = resultCount * batch;
            int added = player.Add(result, total);

            // TryReserve 에서 자리를 확인했으니 보통은 다 들어간다.
            // 그 사이에 가방이 찼다면(다른 곳에서 아이템이 들어왔다든가) 알려준다
            if (added < total)
                Debug.LogWarning($"[제작] {result.DisplayName} {total - added}개가 가방에 못 들어갔습니다.", this);
        }

        /// <summary>연출 없이 한 번에 만든다. 예전 코드 호환용</summary>
        public bool TryCraft(PlayerInventory player, int batch, out string failReason)
        {
            if (!TryReserve(player, batch, out failReason)) return false;

            Deliver(player, batch);
            return true;
        }

        // ════════════════════════════════════════════════════════════

        private void OnValidate()
        {
            resultCount = Mathf.Max(1, resultCount);
            maxBatch = Mathf.Clamp(maxBatch, 1, 10);

            if (materials == null) return;

            // 구조체는 필드 초기화식을 못 쓰니 여기서 기본값을 채운다
            for (int i = 0; i < materials.Length; i++)
            {
                MaterialCost m = materials[i];
                if (m.count <= 0) m.count = 1;

                materials[i] = m;
            }
        }
    }
}