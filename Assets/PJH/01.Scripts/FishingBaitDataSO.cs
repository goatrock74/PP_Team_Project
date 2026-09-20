using UnityEngine;

namespace PJH._01.Scripts
{
    /// <summary>
    /// 낚시 미끼. ItemSO 를 상속하므로 인벤토리·상점·제작이 그대로 다룰 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "FishingBaitDataSO", menuName = "Fishing/Fishing Bait Data", order = 0)]
    public class FishingBaitDataSO : ItemSO
    {
        [Header("미끼 효과")]
        [Tooltip("이 미끼를 쓰는 동안 더해지는 행운. 0이면 확률에 영향이 없다")]
        [SerializeField, Min(0f)] private float fishLuckBonus;

        [Tooltip("몇 번 낚을 때까지 효과가 유지되는가")]
        [SerializeField, Min(1)] private int effectiveCatchCount = 1;

        public float FishLuckBonus => fishLuckBonus;
        public int EffectiveCatchCount => effectiveCatchCount;

        protected override void OnValidate()
        {
            base.OnValidate();

            // 미끼는 무조건 Bait 분류
            itemType = ItemType.Bait;

            // ★ 예전엔 여기서 maxStack 을 99 로 못박아서, 인스펙터에서 20 을 넣어도
            //   저장하는 순간 다시 99 로 돌아갔다. 이제는 값이 없을 때만 기본값을 준다
            if (maxStack <= 1) maxStack = 99;
        }
    }
}