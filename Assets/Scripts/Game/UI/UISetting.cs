using UnityEngine;
using UnityEngine.UI;

public class UISetting : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private IAudioService _audio;

    public void Initialize(IAudioService audio)
    {
        _audio = audio;

        if (bgmSlider == null || sfxSlider == null)
        {
            Debug.LogError("UISetting missing reference!");
            return;
        }

        bgmSlider.value = _audio.GetBGMVolume();
        sfxSlider.value = _audio.GetSFXVolume();

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnBGMChanged(float value)
    {
        _audio.SetBGMVolume(value);
    }

    private void OnSFXChanged(float value)
    {
        _audio.SetSFXVolume(value);
    }
}