using System.Text;
using UnityEngine;
using KSM._00.Scripts.Items;

/// <summary>
/// 상점(PJW)과 인벤토리(KSM)를 잇는 <b>유일한</b> 다리.
///
/// 상점 코드는 PlayerInventory 를 몰라도 되고, 인벤토리 코드는 Item 을 몰라도 된다.
/// 양쪽 구조가 바뀌어도 이 파일 하나만 고치면 된다.
///
/// ── 왜 필요한가 ────────────────────────────────────────────────
/// 상점은 <c>Item.Item_count</c> 로 보유 수량을 보는데, 그 값은 ScriptableObject
/// 안에 들어 있어서 실제 인벤토리와 아무 상관이 없다. 게다가 SO 에 수량을 저장하면
/// 에디터에서는 값이 파일에 눌러앉고 빌드에서는 매번 초기화돼서, 세이브가 안 된다.
///
/// 그래서 <b>수량의 진짜 주인은 PlayerInventory 하나</b>로 두고,
/// <c>Item_count</c> 는 화면에 뿌리기 위한 <b>복사본</b>으로만 쓴다.
/// <see cref="SyncCounts"/> 가 새로 고칠 때마다 덮어쓴다.
///
/// ── 씬 배치 ────────────────────────────────────────────────────
/// 아무 오브젝트에나 하나 붙이고 Map 에 ShopItemMapSO 를 꽂으면 끝.
/// 상점 오브젝트에 같이 붙이면 찾기 편하다.
///
/// ── 상점 쪽에서 쓰는 법 ────────────────────────────────────────
///     ShopInventoryBridge.SyncCounts();                  // 목록 새로 고치기 직전에
///     int have = ShopInventoryBridge.CountOf(item);      // 보유 수량
///     int sold = ShopInventoryBridge.Sell(item, 3, out int gold);   // 팔기
///     int got  = ShopInventoryBridge.Buy(item, 1);       // 사기
/// </summary>
public class ShopInventoryBridge : MonoBehaviour
{
    public enum PriceSource
    {
        /// <summary>상점이 정한 값 (Item.Item_SellPrice). 품질과 무관하게 일정하다</summary>
        Shop = 0,

        /// <summary>인벤토리 값 (ItemSO.GetSellPrice). 좋음·최상이 더 비싸게 팔린다</summary>
        Inventory = 1,
    }

    [Header("참조")]
    [Tooltip("상점 Item ↔ 인벤토리 ItemSO 짝을 담은 표")]
    [SerializeField] private ShopItemMapSO map;

    [Header("설정")]
    [Tooltip("판매가를 어느 쪽 값으로 계산할지")]
    [SerializeField] private PriceSource priceSource = PriceSource.Inventory;

    [Tooltip("팔 때 낮은 품질부터 소모한다. 끄면 최상급부터 나간다")]
    [SerializeField] private bool sellWorstFirst = true;

    [Tooltip("짝이 없는 아이템을 콘솔에 알려준다. 표를 다 채우면 꺼도 된다")]
    [SerializeField] private bool warnUnmapped = true;

    private static ShopInventoryBridge _instance;

    /// <summary>낮은 품질 → 높은 품질 순서</summary>
    private static readonly ItemQuality[] Ascending =
        { ItemQuality.Normal, ItemQuality.Good, ItemQuality.Best };

    private static readonly ItemQuality[] Descending =
        { ItemQuality.Best, ItemQuality.Good, ItemQuality.Normal };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[상점연동] ShopInventoryBridge 가 씬에 두 개 있습니다. 하나만 남기세요.", this);
            return;
        }

        _instance = this;

        if (map == null)
            Debug.LogError("[상점연동] Map 이 비어 있습니다. ShopItemMapSO 를 꽂아주세요.", this);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ════════════════════════════════════════════════════════════
    //  정적 헬퍼 — 다리가 없어도 예외가 안 나게 만들어져 있다
    // ════════════════════════════════════════════════════════════

    /// <summary>상점 아이템에 대응하는 인벤토리 아이템. 짝이 없으면 null</summary>
    public static ItemSO Resolve(Item shopItem)
    {
        if (_instance == null || _instance.map == null || shopItem == null) return null;

        ItemSO so = _instance.map.ToInventory(shopItem);

        if (so == null && _instance.warnUnmapped)
            Debug.LogWarning($"[상점연동] '{shopItem.name}' 에 짝지어진 ItemSO 가 없습니다. " +
                             $"{_instance.map.name} 에 추가해주세요.", shopItem);

        return so;
    }

    /// <summary>지금 갖고 있는 개수 (가방 + 핫바)</summary>
    public static int CountOf(Item shopItem)
    {
        ItemSO so = Resolve(shopItem);
        PlayerInventory inv = PlayerInventory.Instance;

        return so == null || inv == null ? 0 : inv.CountOf(so);
    }

    /// <summary>품질별 개수. 판매창에서 등급을 나눠 보여주고 싶을 때</summary>
    public static int CountOf(Item shopItem, ItemQuality quality)
    {
        ItemSO so = Resolve(shopItem);
        PlayerInventory inv = PlayerInventory.Instance;

        if (so == null || inv == null) return 0;

        return inv.Bag.CountOf(so, quality) + inv.Hotbar.CountOf(so, quality);
    }

    /// <summary>이만큼 팔면 얼마를 받는지 미리 계산한다. 실제로 빼지는 않는다</summary>
    public static int PreviewSellValue(Item shopItem, int amount)
        => Take(shopItem, amount, false, out int gold) > 0 ? gold : 0;

    /// <summary>
    /// 실제로 판다. 인벤토리에서 빼고, 받은 금액을 gold 로 돌려준다.
    /// 반환값은 <b>실제로 팔린 개수</b> — 요청보다 적을 수 있으니 이 값으로 정산할 것.
    /// </summary>
    public static int Sell(Item shopItem, int amount, out int gold)
        => Take(shopItem, amount, true, out gold);

    /// <summary>금액이 필요 없을 때</summary>
    public static int Sell(Item shopItem, int amount) => Sell(shopItem, amount, out _);

    /// <summary>가방에 자리가 있는지. 돈을 차감하기 <b>전에</b> 반드시 확인할 것</summary>
    public static bool CanReceive(Item shopItem, int amount, ItemQuality quality = ItemQuality.Normal)
    {
        ItemSO so = Resolve(shopItem);
        PlayerInventory inv = PlayerInventory.Instance;

        return so != null && inv != null && inv.CanAccept(so, amount, quality);
    }

    /// <summary>산 아이템을 넣는다. 반환값은 실제로 들어간 개수</summary>
    public static int Buy(Item shopItem, int amount, ItemQuality quality = ItemQuality.Normal)
    {
        ItemSO so = Resolve(shopItem);
        PlayerInventory inv = PlayerInventory.Instance;

        if (so == null || inv == null || amount <= 0) return 0;

        int added = inv.Add(so, amount, quality);

        if (_instance != null) _instance.SyncOne(shopItem, so, inv);

        return added;
    }

    /// <summary>
    /// 표에 있는 모든 상점 아이템의 <c>Item_count</c> 를 인벤토리 기준으로 덮어쓴다.
    /// 판매 목록을 새로 그리기 <b>직전에</b> 한 번 부르면, 상점 코드는 예전처럼
    /// <c>Item_count</c> 만 읽어도 실제 보유량이 나온다.
    /// </summary>
    public static void SyncCounts()
    {
        if (_instance == null || _instance.map == null) return;

        PlayerInventory inv = PlayerInventory.Instance;
        if (inv == null) return;

        foreach (Item shopItem in _instance.map.ShopItems)
        {
            ItemSO so = _instance.map.ToInventory(shopItem);
            _instance.SyncOne(shopItem, so, inv);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════════════

    private void SyncOne(Item shopItem, ItemSO so, PlayerInventory inv)
    {
        if (shopItem == null || so == null || inv == null) return;

        shopItem.Item_count = inv.CountOf(so);
    }

    /// <summary>판매 계산. apply 가 false 면 금액만 계산하고 인벤토리는 안 건드린다</summary>
    private static int Take(Item shopItem, int amount, bool apply, out int gold)
    {
        gold = 0;

        ItemSO so = Resolve(shopItem);
        PlayerInventory inv = PlayerInventory.Instance;

        if (so == null || inv == null || amount <= 0) return 0;

        ItemQuality[] order = _instance != null && !_instance.sellWorstFirst ? Descending : Ascending;

        int taken = 0;

        foreach (ItemQuality q in order)
        {
            if (taken >= amount) break;

            int want = amount - taken;
            int have = inv.Bag.CountOf(so, q) + inv.Hotbar.CountOf(so, q);
            int n = Mathf.Min(want, have);

            if (n <= 0) continue;

            if (apply) n = inv.Remove(so, q, n);
            if (n <= 0) continue;

            taken += n;
            gold += UnitPrice(shopItem, so, q) * n;
        }

        if (apply && taken > 0 && _instance != null) _instance.SyncOne(shopItem, so, inv);

        return taken;
    }

    private static int UnitPrice(Item shopItem, ItemSO so, ItemQuality quality)
    {
        bool useInventory = _instance == null || _instance.priceSource == PriceSource.Inventory;

        if (useInventory) return so.GetSellPrice(quality);

        return shopItem != null ? shopItem.Item_SellPrice : 0;
    }

    // ════════════════════════════════════════════════════════════
    //  진단
    // ════════════════════════════════════════════════════════════

    [ContextMenu("상점 연동 진단")]
    private void Diagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══ 상점 연동 진단 ═══");

        sb.AppendLine($"Map : {(map == null ? "★ 비어 있음" : map.name)}");
        sb.AppendLine($"PlayerInventory : {(PlayerInventory.Instance == null ? "★ 씬에 없음" : "O")}");
        sb.AppendLine($"판매가 기준 : {priceSource}");

        if (map == null || PlayerInventory.Instance == null)
        {
            Debug.LogError(sb.ToString(), this);
            return;
        }

        PlayerInventory inv = PlayerInventory.Instance;
        int rows = 0;

        foreach (Item shopItem in map.ShopItems)
        {
            ItemSO so = map.ToInventory(shopItem);
            if (so == null) continue;

            int n = inv.CountOf(so);
            if (n <= 0) continue;

            sb.AppendLine($"  {shopItem.Item_name} ({so.DisplayName}) : {n}개  " +
                          $"일반 {inv.Bag.CountOf(so, ItemQuality.Normal) + inv.Hotbar.CountOf(so, ItemQuality.Normal)} / " +
                          $"좋음 {inv.Bag.CountOf(so, ItemQuality.Good) + inv.Hotbar.CountOf(so, ItemQuality.Good)} / " +
                          $"최상 {inv.Bag.CountOf(so, ItemQuality.Best) + inv.Hotbar.CountOf(so, ItemQuality.Best)}");
            rows++;
        }

        if (rows == 0) sb.AppendLine("  (팔 수 있는 것이 하나도 없습니다)");

        Debug.Log(sb.ToString(), this);
    }
}
