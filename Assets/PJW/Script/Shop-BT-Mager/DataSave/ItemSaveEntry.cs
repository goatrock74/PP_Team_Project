using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSaveEntry
{
    public string itemName;
    public int count;
}

[Serializable]
public class InventorySaveData
{
    public List<ItemSaveEntry> items = new List<ItemSaveEntry>();
}
