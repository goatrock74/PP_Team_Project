using KSM._00.Scripts.Effects;
using UnityEngine;
using KSM._00.Scripts.Items;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 도끼로 벨 수 있는 나무.
///
///   쓰러뜨리면       → 그루터기로 바뀐다. 도끼로 쳐도 화면 아래에 "채집불가"
///   Respawn Days 뒤 → 원래 나무로 돌아온다 (체력도 다시 가득)
///
/// 필요한 것: Collider2D(★ 밑동만 덮게), SpriteRenderer, 이 스크립트.
/// 레이어는 PlayerInteractor 의 Interactable Layer 에 포함된 것으로.
///
/// ★ 나무 뒤로 걸어가게 하려면 — 프리팹을 열고 이 컴포넌트 우클릭 ▸ "나무 뒤로 걸어가게 설정".
///   콜라이더를 밑동 크기로 줄이고 정렬 기준점을 피벗으로 바꿔준다.
///   씬에서 선택하면 노란 선이 보이는데, 플레이어 발이 이 선보다 위에 있으면 나무에 가려진다.
/// </summary>
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

    [Header("재생")]
    [Tooltip("쓰러진 뒤 원래 나무로 돌아오기까지 걸리는 인게임 일수.\n" +
             "0 이면 예전처럼 그루터기 없이 사라진다")]
    [SerializeField, Min(0f)] private float respawnDays = 3f;

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
    private bool _warnedNoStump;

    /// <summary>지금 그루터기 상태인가</summary>
    public bool IsStump => _isStump;

    // ════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer != null)
        {
            _fullSprite = bodyRenderer.sprite;
            _fullColor = bodyRenderer.color;
        }

        // 있으면 칠 때마다 흔들린다. 없어도 된다
        _shaker = GetComponentInChildren<Shaker>();

        _health = maxHealth;
    }

    private void Update()
    {
        if (!_isStump || respawnDays <= 0f) return;

        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return;

        if (farm.CurrentGameDays >= _regrowAtDay) Regrow();
    }

    // ════════════════════════════════════════════════════════════
    //  IChoppable
    // ════════════════════════════════════════════════════════════

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

        // 벌목 마스터리가 도끼 위력을 올려준다. 매니저가 없으면 스탯이 0이라 원래 위력 그대로
        float bonus = MasteryManager.Stat(MasteryStat.ChopDamage);
        int damage = Mathf.Max(1, Mathf.RoundToInt(axe.power * (1f + bonus)));

        _health -= damage;

        // 칠 때마다 나오는 부스러기 (나뭇조각 등)
        if (chipTable != null) GiveLoot(chipTable, 1);

        if (_shaker != null) _shaker.Shake();

        if (_health > 0)
        {
            Debug.Log($"[나무] 남은 체력 {_health}/{maxHealth}  (데미지 {damage})");
            return true;
        }

        // ── 쓰러졌다 ──
        if (dropTable != null) GiveLoot(dropTable, 1 + axe.extraDropRolls);

        // 벌목 경험치는 나무의 최대 체력이 정한다. 몇 대에 쓰러뜨렸는지와 무관하다
        MasteryManager.GainByHealth(MasteryType.Logging, maxHealth);

        if (respawnDays <= 0f)
        {
            Destroy(gameObject);
            return true;
        }

        BecomeStump(in ctx);
        return true;
    }

    // ════════════════════════════════════════════════════════════
    //  그루터기 ↔ 나무
    // ════════════════════════════════════════════════════════════

    private void BecomeStump(in ToolUseContext ctx)
    {
        _isStump = true;

        var farm = ctx.farm != null ? ctx.farm : KSM._00.Scripts.Crop.CropManager.Instance;
        _regrowAtDay = (farm != null ? farm.CurrentGameDays : 0f) + respawnDays;

        // 콜라이더는 그대로 둔다. 밑동 크기 = 그루터기 크기라서
        // 그루터기도 막히고, 다시 자랄 때 플레이어가 나무 속에 끼는 일도 없다
        SetStumpVisual(true);

        Debug.Log($"[나무] 쓰러짐 — {respawnDays:0.#}일 뒤 다시 자랍니다", this);
    }

    private void Regrow()
    {
        _isStump = false;
        _health = maxHealth;

        SetStumpVisual(false);

        if (_shaker != null) _shaker.Shake();    // 다시 자랐다는 신호
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

        // 그루터기 그림이 없으면 나무를 어둡고 반투명하게 — 숨겨서 투명벽이 되는 것보단 낫다
        bodyRenderer.color = new Color(_fullColor.r * 0.6f, _fullColor.g * 0.6f, _fullColor.b * 0.6f, 0.45f);

        if (_warnedNoStump) return;
        _warnedNoStump = true;

        Debug.LogWarning($"[나무] '{name}' 에 Stump Sprite 가 비어 있어서 반투명한 나무로 대신 보여줍니다. " +
                         "TreeNode 의 Stump Sprite 칸을 채워주세요.", this);
    }

    private string BuildStumpMessage()
    {
        if (!showRemainingDays || respawnDays <= 0f) return stumpMessage;

        var farm = KSM._00.Scripts.Crop.CropManager.Instance;
        if (farm == null) return stumpMessage;

        int days = Mathf.Max(1, Mathf.CeilToInt(_regrowAtDay - farm.CurrentGameDays));
        return $"{stumpMessage} ({days}일 뒤 자람)";
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

    // ════════════════════════════════════════════════════════════
    //  씬 뷰 표시 — 정렬 기준선
    // ════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════
    //  에디터 도구
    // ════════════════════════════════════════════════════════════

#if UNITY_EDITOR

    /// <summary>
    /// 콜라이더를 밑동 크기로 줄이고, 정렬 기준점을 피벗으로 바꾼다.
    /// 프리팹을 열어서(더블클릭) 실행하면 씬의 모든 나무에 한 번에 적용된다.
    /// </summary>
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

        // 1) 콜라이더를 BoxCollider2D 하나로 정리한다.
        //    Polygon Collider 는 그림 윤곽을 통째로 따서 나무 전체가 벽이 된다 — 뒤로 못 가는 원인
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) box = Undo.AddComponent<BoxCollider2D>(gameObject);

        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (c == box || c.isTrigger) continue;        // 트리거는 다른 용도일 수 있으니 둔다
            Undo.DestroyObjectImmediate(c);
        }

        // 2) 그림의 아래쪽 가운데에 밑동 크기로 맞춘다 (씬에 안 꺼내도 계산되게 스프라이트 기준)
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

        // 3) 정렬 기준점을 그림 중앙이 아니라 피벗(밑동)으로
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