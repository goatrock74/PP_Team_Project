using UnityEngine;

public class ShopExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ShopEntrance.IsPlayer(collision)) ExitShop();
    }

    public static void ExitShop()
    {
        if (SeasonPassive.Instance != null) SeasonPassive.Instance.RestoreEffectAfterShop();
        if (SoundManager.Instance != null) SoundManager.Instance.ExitShop();
        if (OceanTeleporter.Instance != null)
        {
            OceanTeleporter.Instance.ResumeSeagullsForShop();
            OceanTeleporter.Instance.ResumeOutdoorBGM();
        }
    }
}
