using UnityEngine;

public class PlayerFootStep : MonoBehaviour
{
    [SerializeField] private AudioClip footstepSounds;

    public void PlayFootstepSound()
    { 
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(footstepSounds);
        }
    }
}
