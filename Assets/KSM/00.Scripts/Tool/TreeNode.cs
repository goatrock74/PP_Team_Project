using UnityEngine;
using KSM._00.Scripts.Items;
using KSM._00.Scripts.Effects;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif
[RequireComponent(typeof(Collider2D))]
public class TreeNode : MonoBehaviour, IChoppable
{
    [Header("체력")]
    [Tooltip("이만큼 깎아야 쓰러진다")]
    [SerializeField, Min(1)] private int maxHealth = 3;

    [Tooltip("이 등급 이상의 도끼만 통한다")]
    [SerializeField] private ToolTier requiredTier = ToolTier.Normal;

    [Header("전리품")]
    [Tooltip("쓰러질 때 나오는 것들")]
    [SerializeField] private LootTableSO dropTable;

    [Tooltip("한 번 칠 때마다 나오는 것 (없어도 됨)")]
    [SerializeField] private LootTableSO chipTable;

    [Header("재생 — 최소~최대 일 사이에서 랜덤")]
    [Tooltip("쓰러진 뒤 원래 나무로 돌아오기까지 걸리는 최소 인게임 일수")]
    [FormerlySerializedAs("respawnDays")]
    [SerializeField, Min(0f)] private float minRespawnDays = 2f;

    [Tooltip("최대 일수. 벨 때마다 최소~최대 사이에서 새로 뽑는다.\n" +
             "최소보다 작으면 최소와 같게 맞춰진다 (= 랜덤 없이 고정).\n" +
             "둘 다 0 이면 예전처럼 그루터기 없이 사라진다")]
    [SerializeField, Min(0f)] private float maxRespawnDays = 4f;

    [Header("모습")]
    [Tooltip("나무 그림. 비우면 자신과 자식에서 찾는다")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("쓰러진 뒤 보여줄 그루터기 그림.\n★ 나무 그림과 피벗을 맞출 것 (둘 다 Bottom)")]
    [SerializeField] private Sprite stumpSprite;

    [Tooltip("그루터기일 때 숨길 것들. 잎·그림자가 따로 된 스프라이트면 여기에")]
    [SerializeField] private Renderer[] hideWhenStump;

    [Header("화면 안내 문구 (아래쪽 빨간 글씨)")]
    [SerializeField] private string stumpMessage = "채집불가";
    [SerializeField] private string tierMessage = "더 좋은 도끼가 필요합니다";

    [Tooltip("켜면 그루터기 문구 뒤에 남은 날짜를 붙인다 — 예: 채집불가 (2일 뒤 자람)")]
    [SerializeField] private bool showRemainingDays;

    [Header("밑동 콜라이더 비율 — 우클릭 메뉴가 쓴다")]
    [Tooltip("그림 너비 중 몇 %를 막을지")]
    [SerializeField, Range(0.05f, 1f)] private float trunkWidthRatio = 0.35f;

    [Tooltip("그림 높이 중 아래에서 몇 %를 막을지")]
    [SerializeField, Range(0.02f, 0.5f)] private float trunkHeightRatio = 0.12f;

    private int _health;
    private bool _isStump;
    private float _regrowAtDay;

    private Sprite _fullSprite;
    private Color _fullColor = Color.white;
    private Shaker _shaker;
    private TreeMotion _motion;
    private bool _warnedNoStump;

    private bool _gone;            
    private bool _awake;         
    private bool _hasPending;
    private bool _pendingStump;
    private float _pendingRegrowAt;
    private int _pendingHealth;
    public static event System.Action<TreeNode> AnyStateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AnyStateChanged = null;

    public bool IsStump => _isStump;


    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer != null)
        {
            _fullSprite = bodyRenderer.sprite;
            _fullColor = bodyRenderer.color;
        }

        _motion = GetComponentInChildren<TreeMotion>();
        _shaker = GetComponentInChildren<Shaker>();

        _health = maxHealth;
        _awake = true;

        if (_hasPending)
        {
            _hasPending = false;
            ApplyLoadedState(_pendingStump, _pendingRegrowAt, _pendingHealth);
        }
    }

    private void Update()
    {
        if (!_isStump) return;

        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return;

        if (NowDays(farm) >= _regrowAtDay) Regrow();
    }


    public bool CanChop(AxeSO axe) => axe != null && !_isStump && axe.tier >= requiredTier;

    public bool Chop(AxeSO axe, in ToolUseContext ctx)
    {
        if (axe == null) return false;

        if (_isStump)
        {
            ScreenMessageUI.Show(BuildStumpMessage());
            return false;
        }

        if (axe.tier < requiredTier)
        {
            ScreenMessageUI.Show(tierMessage);
            return false;
        }

        float bonus = MasteryManager.Stat(MasteryStat.ChopDamage);
        int damage = Mathf.Max(1, Mathf.RoundToInt(axe.power * (1f + bonus)));

        _health -= damage;

        if (chipTable != null) GiveLoot(chipTable, 1);

        PlayHitMotion(in ctx);

        if (_health > 0)
        {
            Debug.Log($"[나무] 남은 체력 {_health}/{maxHealth}  (데미지 {damage})");
            AnyStateChanged?.Invoke(this);
            return true;
        }
        if (dropTable != null) GiveLoot(dropTable, 1 + axe.extraDropRolls);

        MasteryManager.GainByHealth(MasteryType.Logging, maxHealth);
        float fallTime = _motion != null ? _motion.Fall(ctx.userPosition.x) : 0f;

        if (!Respawns)
        {
            _gone = true;
            AnyStateChanged?.Invoke(this);

            if (fallTime > 0f)
            {
                HideForever();
                Destroy(gameObject, fallTime + 0.05f);
            }
            else
            {
                Destroy(gameObject);
            }

            return true;
        }

        BecomeStump(in ctx);
        return true;
    }

    private void BecomeStump(in ToolUseContext ctx)
    {
        _isStump = true;

        var farm = ctx.farm != null ? ctx.farm : KSM._00.Scripts.Crop.CropManager.Instance;
        float days = RollRespawnDays();

        _regrowAtDay = NowDays(farm) + days;
        SetStumpVisual(true);

        Debug.Log($"[나무] 쓰러짐 — {days:0.#}일 뒤 다시 자랍니다 (범위 {minRespawnDays:0.#}~{Mathf.Max(minRespawnDays, maxRespawnDays):0.#}일)", this);

        AnyStateChanged?.Invoke(this);
    }

    private void Regrow()
    {
        _isStump = false;
        _health = maxHealth;

        SetStumpVisual(false);

        if (_motion != null) _motion.PlayRegrow();      
        else if (_shaker != null) _shaker.Shake();     

        AnyStateChanged?.Invoke(this);
    }

    private void SetStumpVisual(bool stump)
    {
        if (hideWhenStump != null)
            foreach (Renderer r in hideWhenStump)
                if (r != null) r.enabled = !stump;

        if (bodyRenderer == null) return;

        if (!stump)
        {
            bodyRenderer.sprite = _fullSprite;
            bodyRenderer.color = _fullColor;
            return;
        }

        if (stumpSprite != null)
        {
            bodyRenderer.sprite = stumpSprite;
            return;
        }

        bodyRenderer.color = new Color(_fullColor.r * 0.6f, _fullColor.g * 0.6f, _fullColor.b * 0.6f, 0.45f);

        if (_warnedNoStump) return;
        _warnedNoStump = true;

        Debug.LogWarning($"[나무] '{name}' 에 Stump Sprite 가 비어 있어서 반투명한 나무로 대신 보여줍니다. " +
                         "TreeNode 의 Stump Sprite 칸을 채워주세요.", this);
    }

    private string BuildStumpMessage()
    {
        if (!showRemainingDays || !Respawns) return stumpMessage;

        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return stumpMessage;

        int days = Mathf.Max(1, Mathf.CeilToInt(_regrowAtDay - NowDays(farm)));
        return $"{stumpMessage} ({days}일 뒤 자람)";
    }

    private bool Respawns => Mathf.Max(minRespawnDays, maxRespawnDays) > 0f;

    private float RollRespawnDays()
    {
        float min = Mathf.Max(0f, minRespawnDays);
        float max = Mathf.Max(min, maxRespawnDays);

        return Random.Range(min, max);
    }

    private void OnValidate()
    {
        if (maxRespawnDays < minRespawnDays) maxRespawnDays = minRespawnDays;
    }

    private void PlayHitMotion(in ToolUseContext ctx)
    {
        if (_motion != null) _motion.Sway(ctx.userPosition.x);
        else if (_shaker != null) _shaker.Shake();
    }
    private void HideForever()
    {
        if (bodyRenderer != null) bodyRenderer.enabled = false;

        if (hideWhenStump != null)
            foreach (Renderer r in hideWhenStump)
                if (r != null) r.enabled = false;

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;
    }

    private void GiveLoot(LootTableSO table, int rolls)
    {
        PlayerInventory player = PlayerInventory.Instance;
        if (player == null || table == null || !table.IsUsable) return;

        for (int i = 0; i < rolls; i++)
        {
            LootEntry entry = table.Roll();
            if (entry.IsValid) player.Add(entry.item, entry.RollCount(), entry.quality);
        }
    }
    private static float NowDays(KSM._00.Scripts.Crop.CropManager farm)
    {
        if (farm == null) return 0f;
        return farm.GameClock != null ? farm.GameClock.TotalGameDays : farm.CurrentGameDays;
    }
    public bool IsGone => _gone;

    public bool TryGetSaveState(out bool stump, out float daysLeft, out int health)
    {
        float now = NowDays(KSM._00.Scripts.Crop.CropManager.Instance);
        if (!_awake && _hasPending)
        {
            stump = _pendingStump;
            daysLeft = stump ? Mathf.Max(0f, _pendingRegrowAt - now) : 0f;
            health = _pendingHealth;
            return stump || (health > 0 && health < maxHealth);
        }

        stump = _isStump;
        daysLeft = _isStump ? Mathf.Max(0f, _regrowAtDay - now) : 0f;
        health = _health;

        return _isStump || (_awake && _health < maxHealth);
    }
    public void LoadSaveState(bool gone, bool stump, float daysLeft, int health)
    {
        if (gone)
        {
            _gone = true;
            Destroy(gameObject);
            return;
        }
        if (stump && daysLeft <= 0f) stump = false;

        float regrowAt = NowDays(KSM._00.Scripts.Crop.CropManager.Instance) + Mathf.Max(0f, daysLeft);
        if (!_awake)
        {
            _hasPending = true;
            _pendingStump = stump;
            _pendingRegrowAt = regrowAt;
            _pendingHealth = health;
            return;
        }

        ApplyLoadedState(stump, regrowAt, health);
    }

    private void ApplyLoadedState(bool stump, float regrowAt, int health)
    {
        _health = health > 0 ? Mathf.Min(health, maxHealth) : maxHealth;
        _regrowAtDay = regrowAt;

        if (_isStump == stump) return;

        _isStump = stump;
        SetStumpVisual(stump);      
    }


    private void OnDrawGizmosSelected()
    {
        SpriteRenderer r = bodyRenderer != null ? bodyRenderer : GetComponentInChildren<SpriteRenderer>();
        if (r == null || r.sprite == null) return;

        // 플레이어 발이 이 선보다 위에 있으면 나무 뒤(가려짐), 아래면 나무 앞
        Vector3 p = r.spriteSortPoint == SpriteSortPoint.Pivot ? r.transform.position : r.bounds.center;
        float half = r.bounds.extents.x;

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.95f);
        Gizmos.DrawLine(p + Vector3.left * half, p + Vector3.right * half);
    }

#if UNITY_EDITOR

    [ContextMenu("나무 뒤로 걸어가게 설정")]
    private void SetupWalkBehind()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer == null || bodyRenderer.sprite == null)
        {
            Debug.LogError("[나무] 나무 그림(SpriteRenderer)을 찾지 못했습니다.", this);
            return;
        }

        const string undoName = "나무 뒤로 걸어가게 설정";

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(gameObject);

        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (c == box || c.isTrigger) continue;        
            Undo.DestroyObjectImmediate(c);
        }
        Bounds sb = bodyRenderer.sprite.bounds;
        Vector3 a = transform.InverseTransformPoint(bodyRenderer.transform.TransformPoint(sb.min));
        Vector3 b = transform.InverseTransformPoint(bodyRenderer.transform.TransformPoint(sb.max));

        float left = Mathf.Min(a.x, b.x);
        float right = Mathf.Max(a.x, b.x);
        float bottom = Mathf.Min(a.y, b.y);
        float top = Mathf.Max(a.y, b.y);

        float w = (right - left) * trunkWidthRatio;
        float h = (top - bottom) * trunkHeightRatio;

        Undo.RecordObject(box, undoName);
        box.isTrigger = false;
        box.size = new Vector2(w, h);
        box.offset = new Vector2((left + right) * 0.5f, bottom + h * 0.5f);
        Undo.RecordObject(bodyRenderer, undoName);
        bodyRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

        EditorUtility.SetDirty(box);
        EditorUtility.SetDirty(bodyRenderer);

        if (PrefabUtility.IsPartOfPrefabInstance(this))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bodyRenderer);
        }

        // 4) 결과와 남은 할 일을 알려준다
        var log = new System.Text.StringBuilder($"[나무] '{name}' 설정 완료\n");
        log.AppendLine($"  콜라이더 : {w:0.00} x {h:0.00} (밑동)");
        log.AppendLine("  Sprite Sort Point : Pivot");

        int solid = 0;
        foreach (Collider2D c in GetComponents<Collider2D>())
            if (!c.isTrigger) solid++;

        if (solid > 1)
            log.AppendLine("  ★ 다른 콜라이더가 아직 남아 있습니다. Box Collider 2D 하나만 남기고 직접 지워주세요.");

        float pivot = PivotRatio(bodyRenderer.sprite);

        if (pivot > 0.25f)
            log.AppendLine($"  ★ 나무 그림의 피벗이 아래가 아닙니다 (높이의 {pivot:P0} 지점).\n" +
                           "    스프라이트 에셋의 Pivot 을 Bottom 으로 바꿔야 나무 뒤로 갈 때 순서가 맞습니다.");

        if (stumpSprite != null && Mathf.Abs(PivotRatio(stumpSprite) - pivot) > 0.1f)
            log.AppendLine("  ★ 그루터기 그림과 나무 그림의 피벗이 다릅니다. 둘 다 Bottom 으로 맞추세요.\n" +
                           "    안 그러면 쓰러질 때 그루터기가 엉뚱한 높이에 나타납니다.");

        Debug.Log(log.ToString(), this);
    }

    private static float PivotRatio(Sprite s)
        => s == null || s.rect.height <= 0f ? 0f : s.pivot.y / s.rect.height;

#endif
}