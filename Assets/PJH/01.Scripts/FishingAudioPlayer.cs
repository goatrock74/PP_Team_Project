using System.Collections;
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
        [SerializeField] private AudioClip bobberExitClip;
        [SerializeField] private AudioClip rodLiftClip;
        
        [SerializeField, Min(0f)]
        private float rodLiftDelay = 0.12f;
        
        
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
        public void PlayRetrieve()
        {
            if (sfxSource == null)
                return;

            if (bobberExitClip != null)
            {
                sfxSource.PlayOneShot(
                    bobberExitClip,
                    0.25f
                );
            }

            StartCoroutine(PlayRodLiftDelayed());
        }

        private IEnumerator PlayRodLiftDelayed()
        {
            yield return new WaitForSeconds(rodLiftDelay);

            if (sfxSource != null &&
                rodLiftClip != null)
            {
                sfxSource.PlayOneShot(
                    rodLiftClip,
                    0.35f
                );
            }
        }
    }
}