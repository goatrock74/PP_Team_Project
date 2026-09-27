using UnityEngine;

public class ShopEntrance : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision) 
    {
        Debug.Log("충돌 감지됨: " + collision.name + " / Tag: " + collision.tag);

        if (collision.CompareTag("Player"))
        {
            // 1. 파티클 및 비 소리 숨기기
            if (SeasonPassive.Instance != null)
            {
                SeasonPassive.Instance.HideActiveEffectForShop();
            }

            // 2. 갈매기 소리 정지
            if (OceanTeleporter.Instance != null)
            {
                OceanTeleporter.Instance.PauseSeagullsForShop();
            }

            // 🌟 3. 바다 BGM 끄기
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopBGM();
            }
        }
    }
}
