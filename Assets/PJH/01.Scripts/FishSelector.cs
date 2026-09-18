using UnityEngine;

namespace PJH.Scripts
{
    public class FishSelector : MonoBehaviour
    {
        #region 물고기 확률 데이터

        [System.Serializable]
        public struct FishChance
        {
            public FishDataSO fish;
        }

        #endregion

        #region 인스펙터 연결

        [Header("시간 정보")]
        [SerializeField] private TimeManager timeManager;

        [Header("물고기 확률표")]
        [SerializeField] private FishChance[] fishTable;

        [Header("행운")]
        [Tooltip("켜면 낚시 마스터리의 행운만큼 희귀 물고기가 잘 걸립니다.")]
        [SerializeField] private bool useMasteryLuck = true;

        #endregion

        #region 현재 선택된 물고기

        // 기존 코드에서 이 이름을 사용 중일 수 있어서 그대로 유지
        public FishDataSO selctedFishDataSO { get; private set; }

        #endregion

        #region 외부에서 호출하는 메서드

        public FishDataSO RandomFish(FishCollectionCategory category)
        {
            if (timeManager == null)
            {
                Debug.LogError(
                    "FishSelector에 TimeManager가 연결되지 않았습니다.");

                return null;
            }

            selctedFishDataSO = Draw(category);

            ApplyFishingLuck(category);

            return selctedFishDataSO;
        }

        #endregion

        #region 물고기 추첨

        private FishDataSO Draw(FishCollectionCategory category)
        {
            float totalWeight = GetTotalWeight(category);

            if (totalWeight <= 0f)
            {
                Debug.LogWarning(
                    $"현재 조건에 출현 가능한 물고기가 없습니다. " +
                    $"계절: {timeManager.CurrentSeason}, " +
                    $"시간: {timeManager.CurrentPeriod}");

                return null;
            }

            float pick = Random.Range(0f, totalWeight);
            FishDataSO lastAvailableFish = null;

            foreach (FishChance entry in fishTable)
            {
                if (entry.fish == null)
                {
                    continue;
                }

                if (entry.fish.collectionCategory != category)
                {
                    continue;
                }

                float currentWeight = GetCurrentWeight(entry);

                if (currentWeight <= 0f)
                {
                    continue;
                }

                lastAvailableFish = entry.fish;
                pick -= currentWeight;

                if (pick <= 0f)
                {
                    return entry.fish;
                }
            }

            // float 계산 오차에 대비한 마지막 안전 처리
            return lastAvailableFish;
        }

        private float GetSeasonMultiplier(FishDataSO fish, TimeManager.SeasonPeriod season)
        {
            switch (season)
            {
                case TimeManager.SeasonPeriod.Spring:
                    return fish.SpringMultiplier;
                case TimeManager.SeasonPeriod.Summer:
                    return fish.SummerMultiplier;
                case TimeManager.SeasonPeriod.Autumn:
                    return fish.AutumnMultiplier;
                case TimeManager.SeasonPeriod.Winter:
                    return fish.WinterMultiplier;

                default:
                    return 0f;
            }
        }

        private float GetTimeMultiplier(FishDataSO fish, TimeManager.TimePeriod period)
        {
            switch (period)
            {
                case TimeManager.TimePeriod.Morning:
                    return fish.MorningMultiplier;
                case TimeManager.TimePeriod.Afternoon:
                    return fish.AfternoonMultiplier;
                case TimeManager.TimePeriod.Night:
                    return fish.NightMultiplier;

                default:
                    return 0f;
            }
        }

        private float GetTotalWeight(FishCollectionCategory category)
        {
            if (fishTable == null)
            {
                return 0f;
            }

            float totalWeight = 0f;

            foreach (FishChance entry in fishTable)
            {
                if (entry.fish == null)
                {
                    continue;
                }

                if (entry.fish.collectionCategory != category)
                {
                    continue;
                }

                totalWeight += GetCurrentWeight(entry);
            }

            return totalWeight;
        }

        #endregion

        #region 현재 가중치 계산

        private float GetCurrentWeight(FishChance entry)
        {
            if (entry.fish == null)
            {
                return 0f;
            }

            float seasonMultiplier =
                GetSeasonMultiplier(
                    entry.fish,
                    timeManager.CurrentSeason);

            float timeMultiplier =
                GetTimeMultiplier(
                    entry.fish,
                    timeManager.CurrentPeriod);

            return entry.fish.BaseCatchWeight
                   * seasonMultiplier
                   * timeMultiplier;
        }

        #endregion

        #region 행운 적용

        private void ApplyFishingLuck(FishCollectionCategory category)
        {
            if (!useMasteryLuck ||
                selctedFishDataSO == null)
            {
                return;
            }

            float luck =
                MasteryManager.Stat(MasteryStat.FishLuck);

            if (luck <= 0f)
            {
                return;
            }

            int extraDrawCount = Mathf.FloorToInt(luck);
            float decimalLuck = luck - extraDrawCount;

            if (Random.value < decimalLuck)
            {
                extraDrawCount++;
            }

            for (int i = 0; i < extraDrawCount; i++)
            {
                FishDataSO candidate = Draw(category);

                if (candidate != null &&
                    IsRarer(candidate, selctedFishDataSO))
                {
                    selctedFishDataSO = candidate;
                }
            }
        }

        private bool IsRarer(
            FishDataSO candidate,
            FishDataSO currentFish)
        {
            int candidateRarity =
                (int)candidate.rarity;

            int currentRarity =
                (int)currentFish.rarity;

            if (candidateRarity != currentRarity)
            {
                return candidateRarity > currentRarity;
            }

            return WeightOf(candidate) <
                   WeightOf(currentFish);
        }

        private float WeightOf(FishDataSO fish)
        {
            foreach (FishChance entry in fishTable)
            {
                if (entry.fish == fish)
                {
                    return GetCurrentWeight(entry);
                }
            }

            return float.MaxValue;
        }

        #endregion

    }
}
