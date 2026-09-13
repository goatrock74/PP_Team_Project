using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.U2D.Animation;   // 2D Animation 패키지 (구버전 방식에서만 사용)
using KSM._00.Scripts;
using KSM._00.Scripts.Items;
 
/// <summary>
/// 손에 든 도구에 따라 <b>애니메이션 클립</b>을 갈아끼우고, 사용할 때 재생한다.
/// 플레이어에 붙인다.
///
/// ── 클립 교체 방식 (Clip Override) ─────────────────────────────
/// Animator 상태와 트리거는 도구 종류당 하나만 둔다. 등급이 바뀌면
/// <b>AnimatorOverrideController 안의 클립만</b> 바꿔 끼운다.
///
///   상태 : AxeWood (하나)   ← 이름은 그대로 둬도 된다
///   클립 : AxeWood / AxeIron / AxeGold …  (등급마다 하나)
///
/// ToolSO 에 Base Clip(= Animator 에 원래 꽂힌 것)과 Tier Clip(= 이 등급용)을
/// 넣어주면 된다. 상태·트리거·코드는 등급이 늘어나도 그대로다.
///
/// ★ 이 방식은 Animator 의 상태나 파라미터를 초기화하지 않는다.
///   runtimeAnimatorController 를 통째로 바꾸면 재생 중인 상태가 끊기는데,
///   여기서는 오버라이드 컨트롤러 <b>하나</b>를 계속 쓰고 그 안의 클립만 교체한다.
/// </summary>
public class ToolAnimator : MonoBehaviour
{
    public enum VisualMode
    {
        /// <summary>등급마다 클립을 따로 두고 갈아끼운다 (권장)</summary>
        ClipOverride = 0,
 
        /// <summary>클립 하나로 두고 SpriteLibraryAsset 만 바꾼다 (구버전)</summary>
        SpriteLibrary = 1,
    }
 
    [Header("방식")]
    [SerializeField] private VisualMode visualMode = VisualMode.ClipOverride;
 
    [Header("참조 (비우면 자동으로 찾음)")]
    [SerializeField] private Animator animator;
 
    [Tooltip("Sprite Library 방식에서만 쓴다")]
    [SerializeField] private SpriteLibrary spriteLibrary;
 
    [Tooltip("Sprite Library 방식에서 아무것도 안 들었을 때 돌아갈 라이브러리")]
    [SerializeField] private SpriteLibraryAsset defaultLibrary;
 
    [Header("로그")]
    [Tooltip("어떤 Trigger 를 쐈는지 콘솔에 찍는다")]
    [SerializeField] private bool verboseLog = true;
 
    [Tooltip("클립을 갈아끼울 때마다 콘솔에 찍는다. 그림이 안 바뀔 때 켜기")]
    [SerializeField] private bool logSwap = true;
 
    /// <summary>아직 휘두르는 중인가. 이 동안에는 다시 못 쓴다</summary>
    public bool IsBusy => Time.time < _busyUntil;
 
    private float _busyUntil;
    private PlayerInventory _player;
 
    private AnimatorOverrideController _overrides;
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _pairs = new();
 
    private SpriteResolver[] _resolvers;
 
    // ════════════════════════════════════════════════════════════
    //  수명 주기
    // ════════════════════════════════════════════════════════════
 
    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
 
        if (visualMode == VisualMode.ClipOverride) SetUpOverrideController();
        else SetUpSpriteLibrary();
    }
 
    private void SetUpOverrideController()
    {
        if (animator == null)
        {
            Debug.LogError("[도구] Animator 를 못 찾았습니다.", this);
            return;
        }
 
        RuntimeAnimatorController baseController = animator.runtimeAnimatorController;
 
        if (baseController == null)
        {
            Debug.LogError("[도구] Animator 에 Controller 가 없습니다.", this);
            return;
        }
 
        // 이미 오버라이드 컨트롤러가 꽂혀 있으면 그걸 그대로 쓴다
        _overrides = baseController as AnimatorOverrideController
                     ?? new AnimatorOverrideController(baseController) { name = baseController.name + " (Runtime Override)" };
 
        animator.runtimeAnimatorController = _overrides;
    }
 
    private void SetUpSpriteLibrary()
    {
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
    //  외형 교체
    // ════════════════════════════════════════════════════════════
 
    private void ApplyHeldVisual()
    {
        if (_player == null) return;
 
        ToolSO tool = _player.HeldItem as ToolSO;
 
        if (visualMode == VisualMode.ClipOverride) ApplyClipOverride(tool);
        else ApplySpriteLibrary(tool);
    }
 
    /// <summary>이 도구용 클립으로 갈아끼운다. 다른 도구의 교체는 원래대로 되돌린다</summary>
    private void ApplyClipOverride(ToolSO tool)
    {
        if (_overrides == null) return;
 
        // 1) 먼저 전부 원본으로 되돌린다.
        //    안 그러면 금 도끼를 들었다가 나무 도끼로 바꿔도 금 클립이 남는다
        _overrides.GetOverrides(_pairs);
 
        bool changed = false;
 
        for (int i = 0; i < _pairs.Count; i++)
        {
            if (_pairs[i].Value == null) continue;
 
            _pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(_pairs[i].Key, null);
            changed = true;
        }
 
        // 2) 이 도구가 쓸 클립만 얹는다
        if (tool != null && tool.baseClip != null && tool.tierClip != null && tool.tierClip != tool.baseClip)
        {
            int at = _pairs.FindIndex(p => p.Key == tool.baseClip);
 
            if (at < 0)
            {
                Debug.LogWarning(
                    $"[도구] '{tool.DisplayName}' 의 Base Clip '{tool.baseClip.name}' 이 " +
                    $"Animator 에 없습니다. Animator 상태에 실제로 꽂혀 있는 클립을 넣어주세요.", this);
            }
            else
            {
                _pairs[at] = new KeyValuePair<AnimationClip, AnimationClip>(tool.baseClip, tool.tierClip);
                changed = true;
 
                if (logSwap)
                    Debug.Log($"[도구] 클립 교체 : {tool.baseClip.name} → {tool.tierClip.name} " +
                              $"(손 : {tool.DisplayName})", this);
            }
        }
        else if (logSwap && tool != null && tool.baseClip == null)
        {
            Debug.Log($"[도구] '{tool.DisplayName}' 은 Base Clip 이 비어 있어 원본 클립으로 나옵니다.", this);
        }
 
        if (changed) _overrides.ApplyOverrides(_pairs);
    }
 
    /// <summary>[구버전] 라이브러리만 갈아끼운다</summary>
    private void ApplySpriteLibrary(ToolSO tool)
    {
        if (spriteLibrary == null) return;
 
        SpriteLibraryAsset target = tool != null && tool.heldSpriteLibrary != null
            ? tool.heldSpriteLibrary
            : defaultLibrary;
 
        if (target == null || spriteLibrary.spriteLibraryAsset == target) return;
 
        spriteLibrary.spriteLibraryAsset = target;
 
        if (_resolvers != null)
            foreach (SpriteResolver r in _resolvers)
            {
                if (r == null) continue;
 
                string category = r.GetCategory();
                if (!string.IsNullOrEmpty(category)) r.SetCategoryAndLabel(category, r.GetLabel());
 
                r.ResolveSpriteToSpriteRenderer();
            }
 
        if (logSwap) Debug.Log($"[도구] 라이브러리 교체 → {target.name}", this);
    }
 
    // ════════════════════════════════════════════════════════════
    //  재생
    // ════════════════════════════════════════════════════════════
 
    /// <summary>
    /// 도구 사용 애니메이션 재생. 쿨다운을 걸고, 실제 효과가 언제 나야 하는지 초를 돌려준다.
    /// 바라보는 방향은 PlayerAnimator 가 관리하므로 건드리지 않는다.
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
    //  진단
    // ════════════════════════════════════════════════════════════
 
    [ContextMenu("손에 든 것 다시 적용")]
    private void ForceApply()
    {
        _player = PlayerInventory.Instance;
        ApplyHeldVisual();
    }
 
    [ContextMenu("도구 애니메이션 진단")]
    private void Diagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"═══ 도구 애니메이션 진단 ({visualMode}) ═══");
 
        PlayerInventory player = _player != null ? _player : PlayerInventory.Instance;
        ToolSO tool = player != null ? player.HeldItem as ToolSO : null;
 
        sb.AppendLine($"[1] 손 : {(tool == null ? (player?.HeldItem == null ? "빈손" : player.HeldItem.DisplayName + " (도구 아님)") : tool.DisplayName)}");
 
        if (tool != null)
        {
            sb.AppendLine($"    Base Clip : {(tool.baseClip == null ? "★ 비어 있음" : tool.baseClip.name)}");
            sb.AppendLine($"    Tier Clip : {(tool.tierClip == null ? "★ 비어 있음" : tool.tierClip.name)}");
            sb.AppendLine($"    Trigger   : {tool.animationTrigger}");
        }
 
        if (_overrides == null)
            sb.AppendLine("[2] Override Controller : ★ 없음");
        else
        {
            _overrides.GetOverrides(_pairs);
            sb.AppendLine($"[2] Override Controller : {_overrides.name}   슬롯 {_pairs.Count}개");
 
            foreach (KeyValuePair<AnimationClip, AnimationClip> p in _pairs)
                sb.AppendLine($"    {p.Key.name}  →  {(p.Value == null ? "(원본)" : p.Value.name)}");
        }
 
        if (animator != null)
        {
            AnimatorStateInfo si = animator.GetCurrentAnimatorStateInfo(0);
            sb.AppendLine($"[3] 현재 상태 진행도 : {si.normalizedTime:0.00}   길이 {si.length:0.###}초");
        }
 
        Debug.Log(sb.ToString(), this);
    }
}