using UnityEngine;
 
namespace KSM._00.Scripts.Crafting
{
    [CreateAssetMenu(fileName = "CraftingStationSO", menuName = "SO/Crafting/Station")]
    public class CraftingStationSO : ScriptableObject
    {
        [Header("표시")]
        [Tooltip("제작창 상단에 띄울 이름. 비우면 에셋 이름")]
        public string stationName;
 
        public Sprite icon;
 
        [Header("제작 목록")]
        [Tooltip("여기 넣은 순서대로 목록에 나온다")]
        public CraftingRecipeSO[] recipes;
 
        public string Title => string.IsNullOrWhiteSpace(stationName) ? name : stationName;
 
        public bool IsUsable => recipes != null && recipes.Length > 0;
    }
}