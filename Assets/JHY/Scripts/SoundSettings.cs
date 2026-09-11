using UnityEngine;

[CreateAssetMenu(fileName = "SoundSettings", menuName = "Game/SoundSettings")]
public class SoundSettings : ScriptableObject
{
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;
}
