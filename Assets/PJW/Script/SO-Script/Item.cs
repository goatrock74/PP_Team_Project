using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "new ItemSO", menuName = "PJW/Item/ItemSO")]
public class Item : ScriptableObject
{
    [field: SerializeField] public SeasonType Season { get; private set; }

    [field: SerializeField] public Sprite Item_icon { get; private set; }

    [field: SerializeField] public int Item_price { get; private set; }

    [field:SerializeField] public string Item_name { get;private set; }

    [TextArea][field:SerializeField] public string item_explanation { get; private set; }

    public int Item_count = 0;
}

public enum SeasonType
{
    Autumn,
    Summer,
    Spring,
    Winter
}