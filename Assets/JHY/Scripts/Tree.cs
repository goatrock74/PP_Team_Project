using UnityEngine;

public class Tree : MonoBehaviour
{
    [Header("Tree Settings")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    [Header("Visuals & FX")]
    [SerializeField] private Sprite stumpSprite; // 밑둥 이미지
    [SerializeField] private GameObject woodPrefab; // 드롭될 나무토막
    [SerializeField] private int dropCount = 3;

    private SpriteRenderer spriteRenderer;
    private Collider2D treeCollider;
    private bool isCutDown = false;

    private void Awake()
    {
        currentHp = maxHp;
        spriteRenderer = GetComponent<SpriteRenderer>();
        treeCollider = GetComponent<Collider2D>();
    }

    // 도끼로 때렸을 때 호출
    public void TakeHit(int damage)
    {
        if (isCutDown) return;

        currentHp -= damage;

        // 나무 흔들림 이펙트나 소리 재생 (선택)
        // ShakeTree();

        if (currentHp <= 0)
        {
            CutDown();
        }
    }

    private void CutDown()
    {
        isCutDown = true;

        // 1. 아이템 드롭 (오브젝트 풀링 사용 권장)
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 dropOffset = Random.insideUnitCircle * 0.5f;
            Instantiate(woodPrefab, (Vector2)transform.position + dropOffset, Quaternion.identity);
        }

        // 2. 나무 밑둥으로 변경 및 콜라이더 조정
        spriteRenderer.sprite = stumpSprite;
        // 필요 시 콜라이더 크기를 밑둥에 맞게 축소
    }
}
