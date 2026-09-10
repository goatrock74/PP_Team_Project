using UnityEngine;
 
namespace KSM._00.Scripts.Crafting
{
    /// <summary>
    /// 제작대 한 종류가 어떤 것들을 만들 수 있는지.
    ///
    /// 지금은 대장간 하나뿐이지만, 나중에 요리대·연금대를 추가할 때
    /// <b>이 에셋을 하나 더 만들고 제작대 오브젝트에 꽂기만</b> 하면 된다.
    /// 코드는 한 줄도 안 바뀐다.
    /// </summary>
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