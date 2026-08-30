using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/Item",order = int.MaxValue)]
public class Item : ScriptableObject
{
    [SerializeField] private seasonType season;

    [SerializeField] private string item_name;

    [SerializeField] private SpriteRenderer item_icon;

    [SerializeField] private int item_price;

    [TextArea] [SerializeField] private string item_explanation;
}

public enum seasonType
{
    autumn,
    summer,
    spring,
    winter
}