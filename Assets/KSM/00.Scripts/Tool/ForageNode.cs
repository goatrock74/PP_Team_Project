using UnityEngine;
using KSM._00.Scripts.Items;
 
/// <summary>
/// 긴낫으로 베면 씨앗 같은 게 나오는 야생 풀숲.
///
/// 확률표를 각 노드가 들고 있어서, 숲 풀숲과 해변 풀숲에서 다른 게 나오게 할 수 있다.
/// 비워두면 낫에 설정된 기본 표를 쓴다.
///
/// 필요한 것: Collider2D, SpriteRenderer, 이 스크립트.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ForageNode : MonoBehaviour, IForageable
{
    [Header("전리품")]
    [Tooltip("여기서 나오는 것들. 비우면 낫의 Fallback Table 을 쓴다")]
    [SerializeField] private LootTableSO lootTable;
 
    [Tooltip("이 등급 이상의 낫만 통한다")]
    [SerializeField] private ToolTier requiredTier = ToolTier.Normal;
 
    [Header("재생")]
    [Tooltip("채집 후 다시 자라는 데 걸리는 인게임 일수. 0이면 사라진다")]
    [SerializeField, Min(0f)] private float respawnDays;
 
    [Header("연출")]
    [SerializeField] private SpriteRenderer bodyRenderer;
 
    [Tooltip("재생 대기 중일 때 보여줄 스프라이트 (없으면 그냥 숨긴다)")]
    [SerializeField] private Sprite depletedSprite;
 
    private bool _depleted;
    private float _readyAtDay;
    private Sprite _fullSprite;
 
    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyRenderer != null) _fullSprite = bodyRenderer.sprite;
    }
 
    private void Update()
    {
        if (!_depleted || respawnDays <= 0f) return;
 
        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return;
 
        if (farm.CurrentGameDays >= _readyAtDay) Respawn();
    }
 
    public bool CanForage(ScytheSO scythe)
        => scythe != null && !_depleted && scythe.tier >= requiredTier;
 
    public bool Forage(ScytheSO scythe, in ToolUseContext ctx)
    {
        if (!CanForage(scythe))
        {
            Debug.Log($"[채집] {(_depleted ? "아직 다시 자라지 않았습니다" : "더 좋은 낫이 필요합니다")}");
            return false;
        }
 
        LootTableSO table = lootTable != null ? lootTable : scythe.fallbackTable;
 
        if (table == null || !table.IsUsable)
        {
            Debug.LogWarning("[채집] 확률표가 없습니다. ForageNode 나 낫에 Loot Table 을 넣어주세요.", this);
            return false;
        }
 
        PlayerInventory player = PlayerInventory.Instance;
        if (player == null) return false;
 
        int rolls = 1 + scythe.bonusRolls;
 
        // ── 채집 마스터리 보정 (매니저가 없으면 각각 0 / 1배라 원래대로 동작한다) ──
        float luck = MasteryManager.Stat(MasteryStat.ForageLuck);                    // 희귀한 게 나올 확률
        float yieldMul = MasteryManager.PerkVal(MasteryPerk.BonusForageYield, 1f);   // 15렙: 채집량 배수
 
        bool gotSomething = false;
 
        for (int i = 0; i < rolls; i++)
        {
            LootEntry entry = table.Roll(luck);
            if (!entry.IsValid) continue;
 
            int count = Mathf.Max(1, Mathf.RoundToInt(entry.RollCount() * yieldMul));
            player.Add(entry.item, count, entry.quality);
 
            // 경험치는 "뽑힌 등급" 으로만 정한다. 개수를 곱하지 않는 이유는
            // 15렙 채집량 퍽이 경험치까지 두 배로 만들어 눈덩이처럼 불어나기 때문
            MasteryManager.GainByRarity(MasteryType.Foraging, entry.rarity);
 
            gotSomething = true;
        }
 
        Deplete(ctx);
        return gotSomething;
    }
 
    private void Deplete(in ToolUseContext ctx)
    {
        if (respawnDays <= 0f)
        {
            Destroy(gameObject);
            return;
        }
 
        _depleted = true;
 
        var farm = ctx.farm != null ? ctx.farm : KSM._00.Scripts.Crop.CropManager.Instance;
 
        // 채집 10렙 퍽 — 재생 시간이 배수만큼 줄어든다. 1.5 면 원래의 2/3 시간
        float speedUp = Mathf.Max(0.01f, MasteryManager.PerkVal(MasteryPerk.FastForageRespawn, 1f));
        float wait = respawnDays / speedUp;
 
        _readyAtDay = (farm != null ? farm.CurrentGameDays : 0f) + wait;
 
        if (bodyRenderer == null) return;
 
        if (depletedSprite != null) bodyRenderer.sprite = depletedSprite;
        else bodyRenderer.enabled = false;
    }
 
    private void Respawn()
    {
        _depleted = false;
 
        if (bodyRenderer == null) return;
 
        bodyRenderer.enabled = true;
        if (_fullSprite != null) bodyRenderer.sprite = _fullSprite;
    }
}