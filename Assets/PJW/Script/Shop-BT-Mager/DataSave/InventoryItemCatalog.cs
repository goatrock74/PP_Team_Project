using UnityEngine;

// ItemSO의 ItemId가 중복되어도 에셋 GUID로 정확히 복원한다.
public class InventoryItemCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string id;
        public ItemSO item;
    }
    public Entry[] items;
}
