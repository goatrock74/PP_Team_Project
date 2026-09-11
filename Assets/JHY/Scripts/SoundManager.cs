using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [Header("Sound Settings")]
    [SerializeField] private SoundSettings soundSettings;

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        InitSlider(masterSlider, soundSettings.masterVolume);
        InitSlider(bgmSlider, soundSettings.bgmVolume);
        InitSlider(sfxSlider, soundSettings.sfxVolume);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

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
}
