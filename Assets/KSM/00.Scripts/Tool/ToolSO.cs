
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;   // 2D Animation 패키지 필요
using KSM._00.Scripts.Crop;
 
/// <summary>도구 등급. 등급마다 별도의 SO 에셋을 만든다 (일반 괭이 / 강화 괭이)</summary>
public enum ToolTier
{
    Normal = 0,
    Enhanced = 1,
}
 
/// <summary>도구를 쓸 때 필요한 정보 묶음. PlayerInteractor 가 채워서 넘긴다</summary>
public struct ToolUseContext
{
    /// <summary>마우스가 가리키는 칸 (그리드 도구용)</summary>
    public Vector3Int cell;
 
    /// <summary>그 칸의 월드 중심 (그리드 도구용)</summary>
    public Vector3 worldPoint;
 
    /// <summary>마우스가 가리키는 실제 좌표. 칸에 스냅되지 않는다</summary>
    public Vector3 aimPoint;
 
    /// <summary>
    /// 플레이어가 바라보는 방향 (정규화). 상자 판정 도구는 이 방향 앞에 상자를 놓는다.
    /// 클릭할 때 마우스 쪽으로 몸을 돌리므로 결국 마우스 방향이 된다.
    /// </summary>
    public Vector2 facing;
 
    /// <summary>도구를 쓴 주체 (플레이어)</summary>
    public GameObject user;
 
    /// <summary>도구를 쓴 주체의 위치</summary>
    public Vector3 userPosition;
 
    /// <summary>밭 관리자. 타일 읽기/쓰기와 작물 조회에 쓴다</summary>
    public CropManager farm;
 
    /// <summary>나무·채집물을 찾을 레이어</summary>
    public LayerMask targetLayer;
}
 
/// <summary>
/// 모든 도구의 부모.
///
/// 도구마다 하는 일이 완전히 달라서(타일 갈기 / 물주기 / 나무 베기 / 채집)
/// 하나의 클래스에 switch 로 몰아넣지 않고 각자 Use 를 구현한다.
/// 그러면 새 도구를 추가할 때 PlayerInteractor 를 한 글자도 안 고쳐도 된다.
///
/// 판정 방식은 두 가지다.
///   그리드형 (괭이·물뿌리개) — 칸 단위로 작용. Area Size 로 범위 조절
///   상자형   (도끼·긴낫)     — 마우스 지점의 상자에 닿은 것에 작용. Hit Box Size 로 조절
/// </summary>
public abstract class ToolSO : ItemSO
{
    [Header("도구 공통")]
    public ToolTier tier = ToolTier.Normal;
 
    [Tooltip("[그리드형] 한 번에 작용하는 칸 범위. 1x1 = 한 칸, 3x3 = 아홉 칸")]
    public Vector2Int areaSize = Vector2Int.one;
 
    [Tooltip("[상자형] 판정 상자 크기 (월드 단위)")]
    public Vector2 hitBoxSize = new Vector2(1.2f, 1.2f);
 
    [Tooltip("[상자형] 플레이어 정면으로 이만큼 떨어진 곳이 상자 중심.\n" +
             "0이면 플레이어 발밑, 1이면 한 칸 앞")]
    [Min(0f)] public float hitBoxDistance = 0.8f;
 
    [Tooltip("도구를 쓸 때 콘솔에 로그를 남긴다")]
    public bool verboseLog = true;
 
    [Header("연출")]
    [Tooltip("Animator 의 Trigger 이름. 같은 종류 도구는 등급이 달라도 같은 값을 쓴다\n" +
             "예: 일반 괭이도 강화 괭이도 \"Hoe\"")]
    public string animationTrigger = "Hoe";
 
    [Tooltip("이 도구를 들었을 때 갈아끼울 스프라이트 라이브러리.\n" +
             "애니메이션 클립은 그대로 두고 그림만 바뀐다 — 등급별 외형은 여기로 처리한다")]
    public SpriteLibraryAsset heldSpriteLibrary;
 
    [Tooltip("휘두르기 시작하고 실제 효과가 나기까지의 시간(초).\n" +
             "괭이가 땅에 닿는 순간에 맞추면 자연스럽다")]
    [Min(0f)] public float impactDelay = 0.15f;
 
    [Tooltip("이 시간(초) 동안은 다시 못 쓴다. 애니메이션 길이에 맞추면 된다")]
    [Min(0f)] public float useCooldown = 0.4f;
 
    [Tooltip("쓰는 동안 캐릭터를 제자리에 멈춘다.\n" +
             "괭이질·벌목처럼 발을 딛고 하는 동작은 켜고, 물주기처럼 가벼운 건 꺼도 된다")]
    public bool lockMovementWhileUsing = true;
 
    [Tooltip("멈춰 있을 시간(초). 0이면 Use Cooldown 을 그대로 쓴다")]
    [Min(0f)] public float movementLockDuration;
 
    /// <summary>실제로 캐릭터를 멈춰둘 시간</summary>
    public float LockSeconds => movementLockDuration > 0f ? movementLockDuration : useCooldown;
 
    /// <summary>
    /// 칸이 아니라 상자로 판정하는가. 미리보기 모양도 이걸 보고 결정된다.
    /// 도끼·긴낫이 true 로 덮어쓴다.
    /// </summary>
    public virtual bool UsesHitBox => false;
 
    /// <summary>지금 이 자리에 쓸 수 있는가. 미리보기 색(초록/빨강)을 정하는 데도 쓰인다</summary>
    public abstract bool CanUse(in ToolUseContext ctx);
 
    /// <summary>실제로 사용. 뭐라도 일어났으면 true</summary>
    public abstract bool Use(in ToolUseContext ctx);
 
    // ════════════════════════════════════════════════════════════
    //  그리드 판정 (괭이·물뿌리개)
    // ════════════════════════════════════════════════════════════
 
    /// <summary>영향 범위의 좌하단 원점 (클릭한 칸이 중앙)</summary>
    public Vector3Int GetOrigin(Vector3Int cell) => CropManager.GetOrigin(cell, areaSize);
 
    /// <summary>영향을 받는 모든 칸</summary>
    public IEnumerable<Vector3Int> GetCells(Vector3Int cell)
    {
        Vector3Int origin = GetOrigin(cell);
 
        for (int x = 0; x < areaSize.x; x++)
            for (int y = 0; y < areaSize.y; y++)
                yield return new Vector3Int(origin.x + x, origin.y + y, origin.z);
    }
 
    // ════════════════════════════════════════════════════════════
    //  상자 판정 (도끼·긴낫)
    // ════════════════════════════════════════════════════════════
 
    // 매번 새 배열을 만들지 않도록 공용 버퍼를 쓴다 (클릭마다 GC 가 도는 걸 막는다)
    private static readonly List<Collider2D> HitBuffer = new();
 
    /// <summary>
    /// 판정 상자의 중심. <b>마우스가 아니라 플레이어 정면 앞</b>이다.
    /// 미리보기와 실제 판정이 같은 값을 쓰도록 여기 한 곳에서만 계산한다.
    /// </summary>
    public Vector2 GetHitBoxCenter(in ToolUseContext ctx)
        => (Vector2)ctx.userPosition + ctx.facing * hitBoxDistance;
 
    /// <summary>정면 상자에 닿은 콜라이더들. 반환된 리스트는 다음 호출 때 덮어써진다</summary>
    protected List<Collider2D> OverlapHitBox(in ToolUseContext ctx)
    {
        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = ctx.targetLayer,
            useTriggers = true,
            useDepth = false,
        };
 
        HitBuffer.Clear();
        Physics2D.OverlapBox(GetHitBoxCenter(in ctx), hitBoxSize, 0f, filter, HitBuffer);
 
        return HitBuffer;
    }
 
    /// <summary>상자 안에서 중심에 가장 가까운 T 하나. 나무처럼 하나만 쳐야 할 때</summary>
    protected T FindNearest<T>(in ToolUseContext ctx) where T : class
    {
        Vector2 center = GetHitBoxCenter(in ctx);
 
        T best = null;
        float bestSqr = float.MaxValue;
 
        foreach (Collider2D c in OverlapHitBox(in ctx))
        {
            if (c == null) continue;
 
            T target = c.GetComponentInParent<T>();
            if (target == null) continue;
 
            // 오브젝트 중심이 아니라 콜라이더의 가장 가까운 점으로 잰다.
            // 큰 나무와 작은 나무가 나란히 있을 때 엉뚱한 게 잡히는 걸 막는다
            float sqr = ((Vector2)c.bounds.ClosestPoint(center) - center).sqrMagnitude;
            if (sqr >= bestSqr) continue;
 
            bestSqr = sqr;
            best = target;
        }
 
        return best;
    }
 
    /// <summary>상자 안의 모든 T. 낫처럼 여러 개를 한 번에 쓸어담을 때</summary>
    protected int FindAll<T>(in ToolUseContext ctx, List<T> results) where T : class
    {
        results.Clear();
 
        foreach (Collider2D c in OverlapHitBox(in ctx))
        {
            if (c == null) continue;
 
            T target = c.GetComponentInParent<T>();
 
            // 콜라이더가 여러 개인 오브젝트가 중복으로 들어오지 않게 확인
            if (target == null || results.Contains(target)) continue;
 
            results.Add(target);
        }
 
        return results.Count;
    }
 
    // ════════════════════════════════════════════════════════════
 
    protected void Log(string message)
    {
        if (verboseLog) Debug.Log($"[{DisplayName}] {message}");
    }
 
    protected override void OnValidate()
    {
        base.OnValidate();
 
        itemType = ItemType.Tool;
        maxStack = 1;                          // 도구는 겹쳐 쌓이지 않는다
 
        areaSize.x = Mathf.Max(1, areaSize.x);
        areaSize.y = Mathf.Max(1, areaSize.y);
 
        hitBoxSize.x = Mathf.Max(0.1f, hitBoxSize.x);
        hitBoxSize.y = Mathf.Max(0.1f, hitBoxSize.y);
    }
}
 