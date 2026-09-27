using UnityEngine;
using UnityEngine.Serialization;
using KSM._00.Scripts.Items;

/// <summary>
/// 긴낫으로 베면 씨앗 같은 게 나오는 야생 풀숲.
///
/// 확률표를 각 노드가 들고 있어서, 숲 풀숲과 해변 풀숲에서 다른 게 나오게 할 수 있다.
/// 비워두면 낫에 설정된 기본 표를 쓴다.
///
/// 채집하면 최소~최대 일 사이에서 랜덤한 날짜 뒤에 다시 자란다.
///
/// ★ 저장 — 캤는지, 다시 자라기까지 며칠 남았는지, 캐서 없어졌는지를
///   FarmSaveManager 가 알아서 저장한다. 풀숲 쪽에서 따로 할 일은 없다.
///   풀숲은 '자리' 로 구분하므로, 저장한 뒤에 씬에서 옮기면 그 풀숲은 처음 상태로 돌아간다.
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

    [Header("재생 — 최소~최대 일 사이에서 랜덤")]
    [Tooltip("채집 후 다시 자라기까지 걸리는 최소 인게임 일수")]
    [FormerlySerializedAs("respawnDays")]
    [SerializeField, Min(0f)] private float minRespawnDays;

    [Tooltip("최대 일수. 채집할 때마다 최소~최대 사이에서 새로 뽑는다.\n" +
             "최소보다 작으면 최소와 같게 맞춰진다 (= 랜덤 없이 고정).\n" +
             "둘 다 0 이면 채집 후 사라진다")]
    [SerializeField, Min(0f)] private float maxRespawnDays;

    [Header("연출")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("재생 대기 중일 때 보여줄 스프라이트 (없으면 그냥 숨긴다)")]
    [SerializeField] private Sprite depletedSprite;

    private bool _depleted;
    private float _readyAtDay;
    private Sprite _fullSprite;

    // ── 저장용 ──
    private bool _gone;             // 다시 안 자라는 풀숲을 캐서 없어졌다
    private bool _awake;            // Awake 가 돌았나. 꺼진 채로 시작한 풀숲은 불러온 상태를 켜질 때 적용한다
    private bool _hasPending;
    private float _pendingReadyAt;

    /// <summary>어느 풀숲이든 상태가 바뀌면 (캠 · 다시 자람). 밭 저장이 듣고 1초 안에 저장한다</summary>
    public static event System.Action<ForageNode> AnyStateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AnyStateChanged = null;

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyRenderer != null) _fullSprite = bodyRenderer.sprite;

        _awake = true;

        // 꺼진 채로 시작한 풀숲 — 켜지기 전에 받아 둔 저장 상태를 이제 적용한다
        if (_hasPending)
        {
            _hasPending = false;
            ApplyDepleted(_pendingReadyAt);
        }
    }

    private void Update()
    {
        if (!_depleted) return;

        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return;

        if (NowDays(farm) >= _readyAtDay) Respawn();
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
        if (!Respawns)
        {
            // 다시 안 자라는 풀숲 — 없어진 걸로 저장된다 (다음에 켜도 안 나타난다)
            _gone = true;
            AnyStateChanged?.Invoke(this);

            Destroy(gameObject);
            return;
        }

        var farm = ctx.farm != null ? ctx.farm : KSM._00.Scripts.Crop.CropManager.Instance;

        // 채집 10렙 퍽 — 재생 시간이 배수만큼 줄어든다. 1.5 면 원래의 2/3 시간
        float speedUp = Mathf.Max(0.01f, MasteryManager.PerkVal(MasteryPerk.FastForageRespawn, 1f));
        float wait = RollRespawnDays() / speedUp;

        ApplyDepleted(NowDays(farm) + wait);

        AnyStateChanged?.Invoke(this);
    }

    /// <summary>캔 상태로 만든다. readyAt = 다시 자라는 날짜</summary>
    private void ApplyDepleted(float readyAt)
    {
        _depleted = true;
        _readyAtDay = readyAt;

        if (bodyRenderer == null) return;

        if (depletedSprite != null) bodyRenderer.sprite = depletedSprite;
        else bodyRenderer.enabled = false;
    }

    private void Respawn()
    {
        _depleted = false;

        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = true;
            if (_fullSprite != null) bodyRenderer.sprite = _fullSprite;
        }

        AnyStateChanged?.Invoke(this);
    }

    // ════════════════════════════════════════════════════════════

    /// <summary>다시 자라나는가. 최소·최대가 둘 다 0 이면 채집 후 사라진다</summary>
    private bool Respawns => Mathf.Max(minRespawnDays, maxRespawnDays) > 0f;

    /// <summary>
    /// 이번에 다시 자라기까지 걸릴 일수를 뽑는다.
    /// 소수점까지 랜덤이라, 같은 날 벤 풀숲들이 한꺼번에 튀어나오지 않고 제각각 자란다
    /// </summary>
    private float RollRespawnDays()
    {
        float min = Mathf.Max(0f, minRespawnDays);
        float max = Mathf.Max(min, maxRespawnDays);

        return Random.Range(min, max);
    }

    /// <summary>
    /// 지금 인게임 날짜.
    /// CropManager.CurrentGameDays 는 게임을 켠 직후 몇 초 동안 0 이었다가 시계 값으로 뛰어오른다.
    /// 그 사이에 날짜를 계산하면 풀숲이 켜자마자 다시 자라버려서, 시계가 꽂혀 있으면 시계를 직접 본다
    /// </summary>
    private static float NowDays(KSM._00.Scripts.Crop.CropManager farm)
    {
        if (farm == null) return 0f;
        return farm.GameClock != null ? farm.GameClock.TotalGameDays : farm.CurrentGameDays;
    }

    private void OnValidate()
    {
        // 최대가 최소보다 작으면 최소에 맞춘다 (= 고정 일수)
        if (maxRespawnDays < minRespawnDays) maxRespawnDays = minRespawnDays;
    }

    // ════════════════════════════════════════════════════════════
    //  저장 · 불러오기 — FarmSaveManager 가 부른다
    // ════════════════════════════════════════════════════════════

    /// <summary>캐서 없어졌다 (다시 안 자라는 풀숲)</summary>
    public bool IsGone => _gone;

    /// <summary>
    /// 저장할 상태를 꺼낸다. 멀쩡한 풀숲이면 false — 적을 필요가 없다.
    /// daysLeft = 다시 자라기까지 남은 인게임 일수
    /// </summary>
    public bool TryGetSaveState(out float daysLeft)
    {
        float now = NowDays(KSM._00.Scripts.Crop.CropManager.Instance);

        // 한 번도 안 켜진 풀숲 — 받아 둔 저장 상태를 그대로 돌려준다
        if (!_awake && _hasPending)
        {
            daysLeft = Mathf.Max(0f, _pendingReadyAt - now);
            return true;
        }

        daysLeft = _depleted ? Mathf.Max(0f, _readyAtDay - now) : 0f;
        return _depleted;
    }

    /// <summary>저장본대로 되돌린다. gone 이면 풀숲을 없앤다</summary>
    public void LoadSaveState(bool gone, bool depleted, float daysLeft)
    {
        if (gone)
        {
            _gone = true;
            Destroy(gameObject);
            return;
        }

        // 꺼 둔 사이에 날짜가 다 지났으면 그냥 멀쩡한 풀숲으로 시작한다
        if (!depleted || daysLeft <= 0f) return;

        float readyAt = NowDays(KSM._00.Scripts.Crop.CropManager.Instance) + daysLeft;

        // 꺼진 채로 있어서 Awake 가 아직이다 — 켜질 때 적용한다 (지금 그림을 바꾸면 원래 그림을 잘못 기억한다)
        if (!_awake)
        {
            _hasPending = true;
            _pendingReadyAt = readyAt;
            return;
        }

        ApplyDepleted(readyAt);
    }
}