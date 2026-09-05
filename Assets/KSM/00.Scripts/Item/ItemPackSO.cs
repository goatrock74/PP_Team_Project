using UnityEngine;
 
/// <summary>
/// 뽑기 팩. 손에 들고 화면을 클릭하면 룰렛이 돌아간다.
///
/// 씨드팩 전용이 아니다 — LootTable 에 무엇을 넣느냐에 따라
/// 물고기팩, 요리재료 상자, 도구 상자 무엇이든 된다.
/// </summary>
[CreateAssetMenu(fileName = "PackSO", menuName = "SO/Item/Pack")]
public class ItemPackSO : ItemSO
{
    [Header("뽑기")]
    [Tooltip("이 팩에서 나올 수 있는 것들과 확률")]
    public LootTableSO lootTable;
 
    [Tooltip("한 번 열 때 몇 번 뽑는가")]
    [Min(1)] public int rollCount = 1;
 
    public bool IsUsable => lootTable != null && lootTable.IsUsable;
 
    protected override void OnValidate()
    {
        base.OnValidate();
        rollCount = Mathf.Max(1, rollCount);
    }
}