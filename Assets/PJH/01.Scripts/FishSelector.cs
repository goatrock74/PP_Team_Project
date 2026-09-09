using UnityEngine;
 
namespace PJH.Scripts
{
    /// <summary>
    /// 어떤 물고기가 걸릴지 뽑는다.
    ///
    /// ★ 원본 버그 두 가지를 고쳤다
    ///   1) if (random &lt;= 100) 이 <b>항상 참</b>이라 언제나 fishDataSO[2] 만 나왔다.
    ///      (그리고 물고기가 3마리 미만이면 IndexOutOfRange 로 터졌다)
    ///   2) 확률 순서가 뒤집혀 있었다. 낮은 값부터 검사해야 희귀한 게 드물게 나온다.
    ///
    /// 이제 가중치를 인스펙터에서 물고기별로 정한다. 합이 100 이 아니어도 되고,
    /// 마리 수를 늘려도 코드를 안 고쳐도 된다.
    /// </summary>
    public class FishSelector : MonoBehaviour
    {
        [System.Serializable]
        public struct FishChance
        {
            public FishDataSO fish;
 
            [Tooltip("클수록 자주 나온다. 전체 합 대비 비율이 곧 확률")]
            [Min(1)] public int weight;
        }
 
        [Tooltip("나올 수 있는 물고기와 가중치. 희귀한 물고기일수록 작은 값")]
        [SerializeField] private FishChance[] fishTable;
 
        [Header("행운")]
        [Tooltip("켜면 낚시 마스터리의 행운만큼 희귀한 물고기가 잘 걸린다")]
        [SerializeField] private bool useMasteryLuck = true;
 
        public FishDataSO selctedFishDataSO { get; private set; }
 
        public FishDataSO RandomFish()
        {
            selctedFishDataSO = Draw();
 
            // 행운: 한 번 더 뽑아서 더 희귀한 쪽(가중치가 작은 쪽)을 고른다.
            // 확률표 자체를 건드리지 않아서 비율이 안 망가진다
            float luck = useMasteryLuck ? MasteryManager.Stat(MasteryStat.FishLuck) : 0f;
 
            if (luck > 0f && selctedFishDataSO != null)
            {
                int extra = Mathf.FloorToInt(luck);
                if (Random.value < luck - extra) extra++;
 
                for (int i = 0; i < extra; i++)
                {
                    FishDataSO candidate = Draw();
                    if (candidate != null && WeightOf(candidate) < WeightOf(selctedFishDataSO))
                        selctedFishDataSO = candidate;
                }
            }
 
            return selctedFishDataSO;
        }
 
        // ════════════════════════════════════════════════════════════
 
        private FishDataSO Draw()
        {
            int total = TotalWeight;
            if (total <= 0) return null;
 
            int pick = Random.Range(0, total);
 
            foreach (FishChance entry in fishTable)
            {
                if (entry.fish == null) continue;
 
                pick -= Mathf.Max(1, entry.weight);
                if (pick < 0) return entry.fish;
            }
 
            return null;
        }
 
        private int TotalWeight
        {
            get
            {
                int sum = 0;
                if (fishTable == null) return 0;
 
                foreach (FishChance e in fishTable)
                    if (e.fish != null) sum += Mathf.Max(1, e.weight);
 
                return sum;
            }
        }
 
        private int WeightOf(FishDataSO fish)
        {
            foreach (FishChance e in fishTable)
                if (e.fish == fish) return Mathf.Max(1, e.weight);
 
            return int.MaxValue;
        }
 
        private void OnValidate()
        {
            if (fishTable == null) return;
 
            for (int i = 0; i < fishTable.Length; i++)
            {
                FishChance e = fishTable[i];
                e.weight = Mathf.Max(1, e.weight);
                fishTable[i] = e;
            }
        }
    }
}