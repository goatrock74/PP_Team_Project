using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.PJW.Script.SO_Script
{
    [CreateAssetMenu(fileName = " new Item List SO", menuName = "PJW/Item/ItemListSO")]
    public class ItemListSO : ScriptableObject
    {
        [SerializeField] private SeasonType targetType;

        [field: SerializeField] public ItemSO[] ItemList { get; private set; }
        
    }
}