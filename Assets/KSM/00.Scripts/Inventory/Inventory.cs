using System;
using UnityEngine;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 아이템 보관함. MonoBehaviour 가 아닌 순수 C# 클래스라
    /// 플레이어 가방, 상자, 상점 재고 등 어디에나 새로 만들어 쓸 수 있다.
    ///
    /// ScriptableObject 에는 아무것도 저장하지 않는다. ItemSO 는 읽기 전용 데이터고,
    /// "무엇이 몇 개 있는가" 는 전부 이 클래스가 들고 있다.
    ///
    /// 같은 아이템이라도 품질이 다르면 다른 칸에 쌓인다.
    /// </summary>
    public class Inventory
    {
        private readonly ItemStack[] _slots;
 
        /// <summary>내용이 바뀔 때마다 발생. UI는 이것만 구독하면 된다</summary>
        public event Action OnChanged;
 
        public int Capacity => _slots.Length;
 
        public Inventory(int capacity)
        {
            _slots = new ItemStack[Mathf.Max(1, capacity)];
        }
 
        /// <summary>i번 칸의 내용물. 비어있으면 null</summary>
        public ItemStack GetSlot(int i)
            => (i >= 0 && i < _slots.Length) ? _slots[i] : null;
 
        // ════════════════════════════════════════════════════════════
        //  넣기
        // ════════════════════════════════════════════════════════════
 
        /// <summary>
        /// 아이템을 넣는다. <b>넣지 못하고 남은 개수를 반환</b>한다 (0이면 전부 들어감).
        /// 반환값을 무시하면 가방이 꽉 찼을 때 아이템이 조용히 증발하니 꼭 확인할 것.
        /// </summary>
        public int Add(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return 0;
 
            int remaining = amount;
 
            // 1. 같은 아이템 + 같은 품질이 이미 들어있는 칸부터 채운다
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                ItemStack slot = _slots[i];
                if (slot == null || !slot.Matches(item, quality) || slot.IsFull) continue;
 
                int put = Mathf.Min(slot.SpaceLeft, remaining);
                slot.count += put;
                remaining -= put;
            }
 
            // 2. 그래도 남으면 빈 칸에 새 스택을 만든다
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i] != null) continue;
 
                int put = Mathf.Min(item.maxStack, remaining);
                _slots[i] = new ItemStack(item, put, quality);
                remaining -= put;
            }
 
            if (remaining != amount) OnChanged?.Invoke();
            return remaining;
        }
 
        /// <summary>실제로 넣지 않고, 전부 들어갈 수 있는지만 확인</summary>
        public bool CanAddAll(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return true;
 
            int space = 0;
            foreach (ItemStack slot in _slots)
            {
                space += (slot == null) ? item.maxStack
                       : slot.Matches(item, quality) ? slot.SpaceLeft
                       : 0;
 
                if (space >= amount) return true;
            }
            return false;
        }
 
        // ════════════════════════════════════════════════════════════
        //  빼기
        // ════════════════════════════════════════════════════════════
 
        /// <summary>품질을 가리지 않고 뺀다. <b>실제로 뺀 개수를 반환</b></summary>
        public int Remove(ItemSO item, int amount) => RemoveInternal(item, amount, null);
 
        /// <summary>지정한 품질만 뺀다 (상점 판매 등)</summary>
        public int Remove(ItemSO item, ItemQuality quality, int amount)
            => RemoveInternal(item, amount, quality);
 
        private int RemoveInternal(ItemSO item, int amount, ItemQuality? quality)
        {
            if (item == null || amount <= 0) return 0;
 
            int removed = 0;
 
            // 뒤 칸부터 뺀다. 앞쪽 스택이 온전히 남아서 UI가 덜 흔들린다
            for (int i = _slots.Length - 1; i >= 0 && removed < amount; i--)
            {
                ItemStack slot = _slots[i];
                if (slot == null || slot.item != item) continue;
                if (quality.HasValue && slot.quality != quality.Value) continue;
 
                int take = Mathf.Min(slot.count, amount - removed);
                slot.count -= take;
                removed += take;
 
                if (slot.count <= 0) _slots[i] = null;
            }
 
            if (removed > 0) OnChanged?.Invoke();
            return removed;
        }
 
        /// <summary>특정 칸에서만 뺀다. 손에 든 것을 소모할 때처럼 대상이 확실할 때 쓴다</summary>
        public int RemoveFromSlot(int index, int amount)
        {
            ItemStack slot = GetSlot(index);
            if (slot == null || amount <= 0) return 0;
 
            int take = Mathf.Min(slot.count, amount);
            slot.count -= take;
 
            if (slot.count <= 0) _slots[index] = null;
 
            OnChanged?.Invoke();
            return take;
        }
 
        public void ClearSlot(int index)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null) return;
 
            _slots[index] = null;
            OnChanged?.Invoke();
        }
 
        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
            OnChanged?.Invoke();
        }
 
        // ════════════════════════════════════════════════════════════
        //  조회
        // ════════════════════════════════════════════════════════════
 
        /// <summary>품질 무관 총 개수</summary>
        public int CountOf(ItemSO item)
        {
            if (item == null) return 0;
 
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null && slot.item == item) total += slot.count;
 
            return total;
        }
 
        /// <summary>특정 품질만 센다</summary>
        public int CountOf(ItemSO item, ItemQuality quality)
        {
            if (item == null) return 0;
 
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null && slot.Matches(item, quality)) total += slot.count;
 
            return total;
        }
 
        public bool Has(ItemSO item, int amount = 1) => CountOf(item) >= amount;
 
        /// <summary>가방 전체를 팔았을 때의 금액</summary>
        public int TotalSellValue()
        {
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null) total += slot.TotalSellPrice;
 
            return total;
        }
 
        // ════════════════════════════════════════════════════════════
        //  정리 / 세이브
        // ════════════════════════════════════════════════════════════
 
        /// <summary>두 칸을 바꾼다. 아이템과 품질이 모두 같으면 합칠 수 있는 만큼 합친다</summary>
        public void SwapOrMerge(int a, int b)
        {
            if (a == b) return;
            if (a < 0 || a >= _slots.Length || b < 0 || b >= _slots.Length) return;
 
            ItemStack from = _slots[a];
            ItemStack to = _slots[b];
 
            bool canMerge = from != null && to != null
                         && to.Matches(from.item, from.quality)
                         && !to.IsFull;
 
            if (canMerge)
            {
                int move = Mathf.Min(to.SpaceLeft, from.count);
                to.count += move;
                from.count -= move;
 
                if (from.count <= 0) _slots[a] = null;
            }
            else
            {
                _slots[a] = to;
                _slots[b] = from;
            }
 
            OnChanged?.Invoke();
        }
 
        /// <summary>세이브용 스냅샷. 원본과 분리된 복사본을 준다</summary>
        public ItemStack[] Snapshot()
        {
            var copy = new ItemStack[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
                copy[i] = _slots[i]?.Clone();
 
            return copy;
        }
 
        /// <summary>세이브 로드용</summary>
        public void Restore(ItemStack[] data)
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = (data != null && i < data.Length) ? data[i]?.Clone() : null;
 
            OnChanged?.Invoke();
        }
    }
}
 