namespace KSM._00.Scripts.Items
{
    /// <summary>
    /// 슬롯이 속한 저장소. 가방과 핫바가 <b>별도의 Inventory</b> 라서,
    /// 칸 번호만으로는 어느 쪽인지 알 수 없기 때문에 항상 짝으로 다닌다.
    ///
    /// 새 저장소(상자, 상점 재고 등)를 추가하려면 여기에 한 줄 넣고
    /// PlayerInventory.GetContainer 에 대응만 시켜주면 된다.
    /// </summary>
    public enum SlotArea
    {
        /// <summary>인벤토리 창의 칸들</summary>
        Bag = 0,
 
        /// <summary>화면 하단 핫바. 가방 칸을 차지하지 않는 별도 저장소</summary>
        Hotbar = 1,
    }
}