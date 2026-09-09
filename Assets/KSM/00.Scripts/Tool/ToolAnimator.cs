using UnityEngine;
using UnityEngine.U2D.Animation;   // 2D Animation 패키지 필요
using KSM._00.Scripts;
using KSM._00.Scripts.Items;
 
/// <summary>
/// 손에 든 도구에 따라 그림(SpriteLibrary)을 갈아끼우고, 사용할 때 애니메이션을 재생한다.
/// 플레이어에 붙인다.
///
/// 핵심 아이디어 — <b>애니메이션 클립은 한 벌만 만든다.</b>
/// 괭이질 클립 하나를 만들어두고, 나무 괭이와 다이아 괭이는 SpriteLibraryAsset 만 바꾼다.
/// 그러면 티어가 늘어나도 클립은 늘어나지 않는다.
///
/// 바라보는 방향은 직접 건드리지 않고 PlayerAnimator 에게 부탁한다.
/// 두 스크립트가 같은 파라미터를 각자 쓰면 매 프레임 서로 덮어쓰기 때문이다.
/// </summary>
public class ToolAnimator : MonoBehaviour
{
    [Header("참조 (비우면 자동으로 찾음)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteLibrary spriteLibrary;
 
    [Tooltip("아무것도 안 들었을 때 돌아갈 라이브러리. 비우면 시작 시점 것을 기본으로 잡는다")]
    [SerializeField] private SpriteLibraryAsset defaultLibrary;
 
    [Tooltip("어떤 Trigger 를 쐈는지 콘솔에 찍는다. 애니메이션이 엉뚱하게 나올 때 켜기")]
    [SerializeField] private bool verboseLog = true;
 
    /// <summary>아직 휘두르는 중인가. 이 동안에는 다시 못 쓴다</summary>
    public bool IsBusy => Time.time < _busyUntil;
 
    private SpriteResolver[] _resolvers;
    private float _busyUntil;
    private PlayerInventory _player;
 
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteLibrary == null) spriteLibrary = GetComponentInChildren<SpriteLibrary>();
 
        _resolvers = GetComponentsInChildren<SpriteResolver>(true);
 
        if (defaultLibrary == null && spriteLibrary != null)
            defaultLibrary = spriteLibrary.spriteLibraryAsset;
    }
 
    private void OnEnable()
    {
        _player = PlayerInventory.Instance;
        if (_player == null) return;
 
        _player.OnHeldChanged += ApplyHeldVisual;
        ApplyHeldVisual();
    }
 
    private void OnDisable()
    {
        if (_player != null) _player.OnHeldChanged -= ApplyHeldVisual;
        _player = null;
    }
 
    // ════════════════════════════════════════════════════════════
 
    /// <summary>손에 든 것이 바뀔 때 그림을 갈아끼운다</summary>
    private void ApplyHeldVisual()
    {
        if (spriteLibrary == null || _player == null) return;
 
        SpriteLibraryAsset target = (_player.HeldItem is ToolSO tool && tool.heldSpriteLibrary != null)
            ? tool.heldSpriteLibrary
            : defaultLibrary;
 
        if (target == null || spriteLibrary.spriteLibraryAsset == target) return;
 
        spriteLibrary.spriteLibraryAsset = target;
        RefreshResolvers();
    }
 
    /// <summary>
    /// 도구 사용 애니메이션 재생. 쿨다운을 걸고, 실제 효과가 언제 나야 하는지 초를 돌려준다.
    ///
    /// 바라보는 방향은 건드리지 않는다. 방향은 이동 입력의 좌우가 정하고
    /// PlayerAnimator 가 관리한다.
    /// </summary>
    public float PlayUse(ToolSO tool)
    {
        if (tool == null) return 0f;
 
        _busyUntil = Time.time + tool.useCooldown;
        FireTrigger(tool);
 
        return tool.impactDelay;
    }
 
    private void FireTrigger(ToolSO tool)
    {
        if (animator == null || string.IsNullOrWhiteSpace(tool.animationTrigger)) return;
 
        // 없는 Trigger 를 쏘면 조용히 아무 일도 안 일어난다. 그래서 먼저 확인한다
        if (!HasTrigger(tool.animationTrigger))
        {
            Debug.LogWarning(
                $"[도구] Animator 에 Trigger '{tool.animationTrigger}' 가 없습니다. " +
                $"({tool.DisplayName}) — Animator Parameters 와 철자를 맞춰주세요.", this);
            return;
        }
 
        animator.SetTrigger(tool.animationTrigger);
 
        if (verboseLog)
            Debug.Log($"[도구] {tool.DisplayName} → Trigger '{tool.animationTrigger}' 발동", this);
    }
 
    private bool HasTrigger(string triggerName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
 
        return false;
    }
 
    // ════════════════════════════════════════════════════════════
 
    /// <summary>라이브러리를 바꾼 뒤 실제 렌더러에 반영한다</summary>
    private void RefreshResolvers()
    {
        if (_resolvers == null) return;
 
        foreach (SpriteResolver r in _resolvers)
            if (r != null) r.ResolveSpriteToSpriteRenderer();
    }
}