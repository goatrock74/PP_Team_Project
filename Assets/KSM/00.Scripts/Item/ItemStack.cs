using System;
 
namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 인벤토리 한 칸의 내용물. 빈 칸은 이 객체 자체가 null 이다.
    ///
    /// struct 가 아니라 class 인 이유: 리스트에 담긴 채로 count 를 고쳐야 하는데
    /// struct 면 복사본이 수정돼서 원본이 안 바뀐다.
    ///
    /// 같은 아이템이라도 품질이 다르면 다른 칸에 들어간다.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public ItemSO item;
        public int count;
        public ItemQuality quality;
 
        public ItemStack(ItemSO item, int count, ItemQuality quality = ItemQuality.Normal)
        {
            this.item = item;
            this.count = count;
            this.quality = quality;
        }
 
        public bool IsEmpty => item == null || count <= 0;
 
        /// <summary>이 칸에 더 넣을 수 있는 개수</summary>
        public int SpaceLeft => item == null ? 0 : item.maxStack - count;
 
        public bool IsFull => item != null && count >= item.maxStack;
 
        /// <summary>이 칸에 해당 아이템·품질을 합칠 수 있는가</summary>
        public bool Matches(ItemSO other, ItemQuality otherQuality)
            => item == other && quality == otherQuality;
 
        /// <summary>이 칸 전체의 판매가</summary>
        public int TotalSellPrice => item == null ? 0 : item.GetSellPrice(quality) * count;
 
        public ItemStack Clone() => new ItemStack(item, count, quality);
    }
}