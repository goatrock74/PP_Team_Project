using UnityEngine;

public class ShopExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. 파티클 복구
            if (SeasonPassive.Instance != null)
            {
                SeasonPassive.Instance.RestoreEffectAfterShop();
            }

            // 2. 갈매기 소리 재개
            if (OceanTeleporter.Instance != null)
            {
                OceanTeleporter.Instance.ResumeSeagullsForShop();

                // 🌟 3. 바다 맵이었다면 바다 BGM 다시 재생
                OceanTeleporter.Instance.ResumeOceanBGM();
            }
        }
    }
}
