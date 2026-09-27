using UnityEngine;

public class FishShopBGM : MonoBehaviour
{
    [SerializeField]private AudioClip bgm;
    void Start()
    {
        Debug.Log("물고기 상점 BGM 스크립트 실행됨!");

        if (bgm != null)
        {
            SoundManager.Instance.PlayBGM(bgm);
        }
        else
        {
            Debug.LogError("상점 BGM 오디오 클립이 인스펙터에 안 들어있습니다!");
        }
    }


}
