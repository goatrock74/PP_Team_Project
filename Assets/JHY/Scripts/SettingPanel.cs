using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        if (SoundManager.Instance == null) return;

        InitSlider(masterSlider, SoundManager.Instance.MasterVolume, OnMasterSliderChanged);
        InitSlider(bgmSlider, SoundManager.Instance.BgmVolume, OnBGMSliderChanged);
        InitSlider(sfxSlider, SoundManager.Instance.SfxVolume, OnSFXSliderChanged);
    }

    private void InitSlider(Slider slider, float initialValue, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        if (slider == null) return;

        slider.onValueChanged.RemoveAllListeners();

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = initialValue;

        slider.onValueChanged.AddListener(onValueChanged);
    }

    private void OnMasterSliderChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMSliderChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(value);
    }
}
