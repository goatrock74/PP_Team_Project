using UnityEngine;

namespace PJH._01.Scripts
{
    public class FishingAudioPlayer : MonoBehaviour
    {
        [Header("오디오 소스")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource reelLoopSource;

        [Header("낚시 효과음")]
        [SerializeField] private AudioClip castClip;
        [SerializeField] private AudioClip splashClip;
        [SerializeField] private AudioClip biteClip;
        [SerializeField] private AudioClip catchClip;
        
        
        public void PlayCast()
        {
            sfxSource.PlayOneShot(castClip, 0.65f);
        }

        public void PlaySplash()
        {
            sfxSource.PlayOneShot(splashClip, 0.55f);
        }

        public void PlayBite()
        {
            sfxSource.PlayOneShot(biteClip, 0.75f);
        }

        public void PlayCatch()
        {
            sfxSource.PlayOneShot(catchClip, 0.85f);
        }
        

        public void StartReel()
        {
            if (!reelLoopSource.isPlaying)
                reelLoopSource.Play();
        }

        public void StopReel()
        {
            reelLoopSource.Stop();
        }
    }
}