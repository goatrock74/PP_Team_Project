using UnityEngine;

public class PlayerAxe : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private int axeDamage = 1;

    public void UseAxe(Vector2 attackDirection)
    {
        // 플레이어가 바라보는 방향으로 Raycast 또는 OverlapCircle
        RaycastHit2D hit = Physics2D.Raycast(transform.position, attackDirection, attackRange, treeLayer);

        if (hit.collider != null)
        {
            if (hit.collider.TryGetComponent<Tree>(out Tree tree))
            {
                tree.TakeHit(axeDamage);
            }
        }
    }
}
