using UnityEngine;

public class FishShopBGM : MonoBehaviour
{
    [SerializeField]private AudioClip bgm;
    void Start()
    {
        SoundManager.Instance.PlayBGM(bgm);
    }

  
}
