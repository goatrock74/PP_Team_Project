using System;
using UnityEngine;
 
namespace KSM._00.Scripts.Items
{
    public class Inventory
    {
        private readonly ItemStack[] _slots;
 
        public event Action OnChanged;
 
        public int Capacity => _slots.Length;
 
        public Inventory(int capacity)
        {
            _slots = new ItemStack[Mathf.Max(1, capacity)];
        }
 
        public ItemStack GetSlot(int i)
            => (i >= 0 && i < _slots.Length) ? _slots[i] : null;
        public int Add(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null || amount <= 0) return 0;
 
            int remaining = amount;
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                ItemStack slot = _slots[i];
                if (slot == null || !slot.Matches(item, quality) || slot.IsFull) continue;
 
                int put = Mathf.Min(slot.SpaceLeft, remaining);
                slot.count += put;
                remaining -= put;
            }
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
 
        public int SpaceFor(ItemSO item, ItemQuality quality = ItemQuality.Normal)
        {
            if (item == null) return 0;
 
            int space = 0;
            foreach (ItemStack slot in _slots)
            {
                space += (slot == null) ? item.maxStack
                       : slot.Matches(item, quality) ? slot.SpaceLeft
                       : 0;
            }
 
            return space;
        }
        public bool CanAddAll(ItemSO item, int amount, ItemQuality quality = ItemQuality.Normal)
            => item == null || amount <= 0 || SpaceFor(item, quality) >= amount;
 
        public bool HasEmptySlot()
        {
            foreach (ItemStack slot in _slots)
                if (slot == null) return true;
 
            return false;
        }
 
        public int Remove(ItemSO item, int amount) => RemoveInternal(item, amount, null);
 
        public int Remove(ItemSO item, ItemQuality quality, int amount)
            => RemoveInternal(item, amount, quality);
 
        private int RemoveInternal(ItemSO item, int amount, ItemQuality? quality)
        {
            if (item == null || amount <= 0) return 0;
 
            int removed = 0;
 
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
        public int CountOf(ItemSO item)
        {
            if (item == null) return 0;
 
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null && slot.item == item) total += slot.count;
 
            return total;
        }
        public int CountOf(ItemSO item, ItemQuality quality)
        {
            if (item == null) return 0;
 
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null && slot.Matches(item, quality)) total += slot.count;
 
            return total;
        }
 
        public bool Has(ItemSO item, int amount = 1) => CountOf(item) >= amount;
        public int TotalSellValue()
        {
            int total = 0;
            foreach (ItemStack slot in _slots)
                if (slot != null) total += slot.TotalSellPrice;
 
            return total;
        }
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
        public static void MoveOrSwap(Inventory fromInv, int a, Inventory toInv, int b)
        {
            if (fromInv == null || toInv == null) return;
 
            // 같은 보관함이면 기존 로직 그대로
            if (ReferenceEquals(fromInv, toInv)) { fromInv.SwapOrMerge(a, b); return; }
 
            if (a < 0 || a >= fromInv._slots.Length) return;
            if (b < 0 || b >= toInv._slots.Length) return;
 
            ItemStack from = fromInv._slots[a];
            if (from == null) return;             
 
            ItemStack to = toInv._slots[b];
 
            bool canMerge = to != null
                         && to.Matches(from.item, from.quality)
                         && !to.IsFull;
 
            if (canMerge)
            {
                int move = Mathf.Min(to.SpaceLeft, from.count);
                to.count += move;
                from.count -= move;
 
                if (from.count <= 0) fromInv._slots[a] = null;
            }
            else
            {
                fromInv._slots[a] = to;
                toInv._slots[b] = from;
            }
 
            fromInv.OnChanged?.Invoke();
            toInv.OnChanged?.Invoke();
        }
        public ItemStack[] Snapshot()
        {
            var copy = new ItemStack[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
                copy[i] = _slots[i]?.Clone();
 
            return copy;
        }
        public void Restore(ItemStack[] data)
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = (data != null && i < data.Length) ? data[i]?.Clone() : null;
 
            OnChanged?.Invoke();
        }
    }
}
 