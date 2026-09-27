using UnityEngine;

public class PlantShopBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgm;
    void Start()
    {
        // Embedded base-scene shops use DialogueManager's temporary shop music.
        if (string.Equals(gameObject.scene.name, "0.1BaseScene", System.StringComparison.OrdinalIgnoreCase)) return;
        SoundManager.Instance.PlayBGM(bgm);
    }

}
