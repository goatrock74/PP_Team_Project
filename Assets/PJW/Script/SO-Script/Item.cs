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

    [TextArea] [SerializeField] private string item_explanation;

    [field:SerializeField] public bool Itme_bought;
}

public enum SeasonType
{
    Autumn,
    Summer,
    Spring,
    Winter
}