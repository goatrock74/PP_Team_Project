using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sound Settings")]
    [SerializeField] private SoundSettings soundSettings;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public float MasterVolume => soundSettings.masterVolume;
    public float BgmVolume => soundSettings.bgmVolume;
    public float SfxVolume => soundSettings.sfxVolume;
    private AudioClip previousBGM;        // 이전 BGM 백업용
    private bool isBGMOverridden = false;
    public bool IsInsideShop { get; private set; }
    private AudioClip outdoorBGM;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        soundSettings.masterVolume = PlayerPrefs.GetFloat("MasterVolume", soundSettings.masterVolume);
        soundSettings.bgmVolume = PlayerPrefs.GetFloat("BGMVolume", soundSettings.bgmVolume);
        soundSettings.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", soundSettings.sfxVolume);

        SetMasterVolume(soundSettings.masterVolume);
        SetBGMVolume(soundSettings.bgmVolume);
        SetSFXVolume(soundSettings.sfxVolume);
    }
    public void OverrideBGM(AudioClip newClip)
    {
        if (newClip == null || bgmSource == null) return;

        if (!isBGMOverridden)
        {
            previousBGM = bgmSource.clip; // 지금 재생 중인 노래 백업
            isBGMOverridden = true;
        }

        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // 2. 상점 퇴장 시: 아까 백업해 둔 원래 노래로 복구
    public void RestoreBGM()
    {
        if (!isBGMOverridden) return;
        AudioClip clip = previousBGM;
        isBGMOverridden = false;
        previousBGM = null;
        if (IsInsideShop) StopBGM();
        else if (clip != null) PlayBGM(clip);
        else StopBGM();
    }
    public void EnterShop()
    {
        if (IsInsideShop) return;
        IsInsideShop = true;
        outdoorBGM = isBGMOverridden ? previousBGM : bgmSource != null ? bgmSource.clip : null;
        if (!isBGMOverridden) StopBGM();
    }

    public void ResetOutdoorSceneAudio()
    {
        IsInsideShop = false;
        isBGMOverridden = false;
        previousBGM = null;
        outdoorBGM = null;
        StopBGM();
        StopSFX();
    }

    public void ExitShop()
    {
        if (!IsInsideShop) return;
        IsInsideShop = false;
        isBGMOverridden = false;
        previousBGM = null;
        AudioClip clip = outdoorBGM;
        outdoorBGM = null;
        if (clip != null) PlayBGM(clip);
        else StopBGM();
    }
    public void SetMasterVolume(float value)
    {
        soundSettings.masterVolume = value;
        audioMixer.SetFloat("Master", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterVolume", value); 
    }

    public void SetBGMVolume(float value)
    {
        soundSettings.bgmVolume = value;
        audioMixer.SetFloat("BGM", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        soundSettings.sfxVolume = value;
        audioMixer.SetFloat("SFX", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (IsInsideShop)
        {
            outdoorBGM = clip;
            return;
        }
        if (isBGMOverridden)
        {
            previousBGM = clip;
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    public void StopSFX()
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }
}
