using UnityEngine;
using PJH.Scripts;
using KSM._00.Scripts.Items;
 
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
 
        if (!player.CanAccept(fish, 1, quality))
        {
            Debug.LogWarning($"[낚시연동] 가방이 가득 차서 {fish.DisplayName} 을(를) 놓쳤습니다.", this);
            return;
        }
 
        player.Add(fish, 1, quality);
 
        MasteryManager.GainByRarity(MasteryType.Fishing, fish.rarity);
 
        if (verboseLog)
            Debug.Log($"[낚시연동] {fish.DisplayName} ({ItemQualityUtil.DisplayName(quality)}) 획득", this);
    }
 
    private ItemQuality RollQuality(FishDataSO fish)
    {
        if (!rollQuality) return ItemQuality.Normal;
 
        float bonus = MasteryManager.Stat(MasteryStat.FishLuck)
                    + fish.difficulty * difficultyQualityBonus;
 
        return qualityChance.Roll(bonus);
    }
}