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
    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    private void Awake()
    {
        // 싱글톤 설정
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

        InitSlider(masterSlider, soundSettings.masterVolume);
        InitSlider(bgmSlider, soundSettings.bgmVolume);
        InitSlider(sfxSlider, soundSettings.sfxVolume);

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        SetMasterVolume(soundSettings.masterVolume);
        SetBGMVolume(soundSettings.bgmVolume);
        SetSFXVolume(soundSettings.sfxVolume);
    }

    private void InitSlider(Slider slider, float value)
    {
        if (slider == null) return;
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = value;
    }

    public void SetMasterVolume(float value)
    {
        soundSettings.masterVolume = value;
        audioMixer.SetFloat("Master", Mathf.Log10(value) * 20);
    }

    public void SetBGMVolume(float value)
    {
        soundSettings.bgmVolume = value;
        audioMixer.SetFloat("BGM", Mathf.Log10(value) * 20);
    }

    public void SetSFXVolume(float value)
    {
        soundSettings.sfxVolume = value;
        audioMixer.SetFloat("SFX", Mathf.Log10(value) * 20);
    }
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

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
}
