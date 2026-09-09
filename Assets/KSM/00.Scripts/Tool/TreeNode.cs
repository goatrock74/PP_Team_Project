using UnityEngine;
using KSM._00.Scripts.Items;
 
/// <summary>
/// 도끼로 벨 수 있는 나무. 아직 나무 에셋이 없으니 일단 이걸로 테스트하면 된다.
///
/// 필요한 것: Collider2D (도구가 이걸로 찾는다), SpriteRenderer, 그리고 이 스크립트.
/// 레이어를 하나 파서(예: Interactable) PlayerInteractor 의 Interactable Layer 에 지정할 것.
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
 
    private int _health;
 
    private void Awake()
    {
        _health = maxHealth;
    }
 
    public bool CanChop(AxeSO axe) => axe != null && axe.tier >= requiredTier && _health > 0;
 
    public bool Chop(AxeSO axe, in ToolUseContext ctx)
    {
        if (!CanChop(axe))
        {
            Debug.Log($"[나무] {(_health <= 0 ? "이미 쓰러짐" : "더 좋은 도끼가 필요합니다")}");
            return false;
        }
 
        // 벌목 마스터리가 도끼 위력을 올려준다. 매니저가 없으면 스탯이 0이라 원래 위력 그대로
        float bonus = MasteryManager.Stat(MasteryStat.ChopDamage);
        int damage = Mathf.Max(1, Mathf.RoundToInt(axe.power * (1f + bonus)));
 
        _health -= damage;
 
        // 칠 때마다 나오는 부스러기 (나뭇조각 등)
        if (chipTable != null) GiveLoot(chipTable, 1);
 
        if (_health > 0)
        {
            Debug.Log($"[나무] 남은 체력 {_health}/{maxHealth}  (데미지 {damage})");
            return true;
        }
 
        // 쓰러졌다
        if (dropTable != null) GiveLoot(dropTable, 1 + axe.extraDropRolls);
 
        // 벌목 경험치는 나무의 최대 체력이 정한다. 큰 나무일수록 많이 준다.
        // 몇 대에 쓰러뜨렸는지와 무관하게 같은 나무는 같은 경험치를 준다
        MasteryManager.GainByHealth(MasteryType.Logging, maxHealth);
 
        Destroy(gameObject);
        return true;
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
 
}
 