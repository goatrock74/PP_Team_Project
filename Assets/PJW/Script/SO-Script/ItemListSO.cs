using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.PJW.Script.SO_Script
{
    [CreateAssetMenu(fileName = " new Item List SO", menuName = "PJW/Item/ItemListSO")]
    public class ItemListSO : ScriptableObject
    {
        [SerializeField] private SeasonType targetType;
        [field: SerializeField] public Item[] ItemList { get; private set; }

        private void OnValidate()
        {
            for (int i = 0; i < ItemList.Length; ++i)
            {
                if (ItemList[i] == null)
                    continue;
                if (ItemList[i].Season != targetType )
                {
                    Debug.LogError($"{name}의 목록에 맞지 않는 타입이 있습니다. {ItemList[i].name}");
                    ItemList[i] = null;
                }
            }
        }
    }
}