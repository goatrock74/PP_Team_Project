using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.PJW.Script.SO_Script
{
    [CreateAssetMenu(fileName = " new Item List SO", menuName = "PJW/Item/ItemListSO")]
    public class ItemListSO : ScriptableObject
    {
        [SerializeField] private SeasonType targetType;
        public SeasonType TargetSeason => targetType;

        // ★ ItemSO 가 아니라 Item. 상점 진열 목록이다
        [field: SerializeField] public Item[] ItemList { get; private set; }
    }
}
