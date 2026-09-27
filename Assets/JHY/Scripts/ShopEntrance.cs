using UnityEngine;

public class ShopEntrance : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayer(collision)) EnterShop();
    }

    internal static bool IsPlayer(Collider2D collision)
    {
        return collision.CompareTag("Player") ||
            (collision.attachedRigidbody != null && collision.attachedRigidbody.CompareTag("Player")) ||
            collision.transform.root.CompareTag("Player");
    }

    public static void EnterShop()
    {
        if (SeasonPassive.Instance != null) SeasonPassive.Instance.HideActiveEffectForShop();
        if (OceanTeleporter.Instance != null) OceanTeleporter.Instance.PauseSeagullsForShop();
        if (SoundManager.Instance != null) SoundManager.Instance.EnterShop();
    }
}
