using UnityEngine;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 물고기 데이터. <b>ItemSO 를 상속</b>해서 인벤토리·핫바·상점·정보창에 그대로 들어간다.
    ///
    /// ★ displayName / icon / basePrice 를 여기서 다시 선언하면 안 된다.
    ///   ItemSO 에 이미 있는데 같은 이름으로 또 선언하면 부모 것을 <b>가려버려서</b>
    ///   인벤토리가 읽는 ItemSO.displayName 과 ItemSO.icon 은 텅 빈 채로 남는다.
    ///   (이름이 파일명으로 나오고 아이콘이 안 보이는 증상)
    /// </summary>
    [CreateAssetMenu(fileName = "Fish Data", menuName = "FishDataSO", order = 0)]
    public class FishDataSO : ItemSO
    {
        [Header("primary information")]
        [Tooltip("낚시 시스템 내부용 ID. 인벤토리는 ItemSO 의 Item Id 를 쓴다")]
        public string fishID;
 
        // displayName  → ItemSO 에 있음
        // icon         → ItemSO 에 있음
        // basePrice    → ItemSO 의 Sell Price 를 쓴다 (품질 배수까지 자동 적용)
        [Header("등급")]
        [Tooltip("낚시 마스터리 경험치가 이 등급으로 정해진다")]
        public ItemRarity rarity = ItemRarity.Common;
 
        [Header("난이도")]
        [Tooltip("미니게임 난이도. 품질 보정에도 쓸 수 있다")]
        public float difficulty;
 
        [Header("MiniGame Movement")]
        [SerializeField, Min(0.01f)]
        private float moveSpeed = 0.25f;
 
        [SerializeField, Min(0.1f)]
        private float minTargetChangeTime = 1f;
 
        [SerializeField, Min(0.1f)]
        private float maxTargetChangeTime = 2f;
 
        [SerializeField, Range(0f, 1f)]
        private float maxMoveDistance = 0.3f;
 
        public float MoveSpeed => moveSpeed;
        public float MinTargetChangeTime => minTargetChangeTime;
        public float MaxTargetChangeTime => maxTargetChangeTime;
        public float MaxMoveDistance => maxMoveDistance;
 
        protected override void OnValidate()
        {
            base.OnValidate();
 
            itemType = ItemType.Fish;   // 인벤토리 정보창에 "물고기" 로 표시된다
 
            if (maxTargetChangeTime < minTargetChangeTime)
                maxTargetChangeTime = minTargetChangeTime;
        }
    }
}