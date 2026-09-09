using UnityEngine;
using PJH.Scripts;
using KSM._00.Scripts.Items;
 
/// <summary>
/// 낚시 시스템과 인벤토리 · 마스터리를 잇는 <b>유일한</b> 다리.
///
/// 친구 코드는 한 줄도 안 고친다. FishingMiniGame 이 이미 쏘고 있는
/// OnFishingSucceeded 이벤트를 그냥 옆에서 같이 구독할 뿐이다.
/// (FishingController 도 같은 이벤트를 듣고 있는데, 구독자는 여럿이어도 된다)
///
/// 씬 배치: FishingMiniGame 이 붙어 있는 오브젝트에 같이 붙이면 편하다.
/// </summary>
public class FishingInventoryAdapter : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비워두면 씬에서 자동으로 찾는다")]
    [SerializeField] private FishingMiniGame miniGame;
 
    [Header("품질")]
    [Tooltip("끄면 잡은 물고기가 전부 '일반' 등급으로 들어간다")]
    [SerializeField] private bool rollQuality = true;
 
    [Tooltip("물고기 품질 확률. 작물의 Quality Chance 와 같은 구조다")]
    [SerializeField] private QualityChance qualityChance = QualityChance.Default;
 
    [Tooltip("난이도가 높은 물고기일수록 좋은 품질이 나올 확률.\n" +
             "0.05 면 difficulty 1당 +5%p. 0이면 난이도 무시")]
    [SerializeField, Min(0f)] private float difficultyQualityBonus;
 
    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;
 
    private void OnEnable()
    {
        if (miniGame == null) miniGame = FindFirstObjectByType<FishingMiniGame>(FindObjectsInactive.Include);
 
        if (miniGame == null)
        {
            Debug.LogWarning("[낚시연동] 씬에서 FishingMiniGame 을 찾지 못했습니다.", this);
            return;
        }
 
        miniGame.OnFishingSucceeded += HandleCaught;
    }
 
    private void OnDisable()
    {
        if (miniGame != null) miniGame.OnFishingSucceeded -= HandleCaught;
    }
 
    // ════════════════════════════════════════════════════════════
 
    private void HandleCaught(FishDataSO fish)
    {
        if (fish == null) return;
 
        PlayerInventory player = PlayerInventory.Instance;
 
        if (player == null)
        {
            Debug.LogWarning("[낚시연동] 씬에 PlayerInventory 가 없어 물고기를 넣지 못했습니다.", this);
            return;
        }
 
        ItemQuality quality = RollQuality(fish);
 
        // 가방이 가득 찼으면 알려주고 끝낸다. 조용히 사라지는 게 제일 나쁘다
        if (!player.CanAccept(fish, 1, quality))
        {
            Debug.LogWarning($"[낚시연동] 가방이 가득 차서 {fish.DisplayName} 을(를) 놓쳤습니다.", this);
            return;
        }
 
        player.Add(fish, 1, quality);
 
        // 낚시 마스터리 경험치는 물고기 등급으로 정해진다.
        // 씬에 MasteryManager 가 없으면 아무 일도 일어나지 않는다
        MasteryManager.GainByRarity(MasteryType.Fishing, fish.rarity);
 
        if (verboseLog)
            Debug.Log($"[낚시연동] {fish.DisplayName} ({ItemQualityUtil.DisplayName(quality)}) 획득", this);
    }
 
    private ItemQuality RollQuality(FishDataSO fish)
    {
        if (!rollQuality) return ItemQuality.Normal;
 
        // 마스터리의 낚시 행운 + 물고기 난이도만큼 좋은 등급 확률이 오른다
        float bonus = MasteryManager.Stat(MasteryStat.FishLuck)
                    + fish.difficulty * difficultyQualityBonus;
 
        return qualityChance.Roll(bonus);
    }
}