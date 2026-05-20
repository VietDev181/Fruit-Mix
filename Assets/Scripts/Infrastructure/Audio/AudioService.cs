using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioService : MonoBehaviour, IAudioService
{
    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const float MutedDb = -80f;
    private const float MinLinear = 0.0001f;

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip gameBGM;
    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip gameOverBGM;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    void Awake()
    {
        LoadVolume();
    }

    public void PlayClickSFX()
    {
        if (clickClip != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }

    public void PlayGameBGM()
    {
        bgmSource.clip = gameBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayMainMenuBGM()
    {
        bgmSource.clip = mainMenuBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayGameOverBGM()
    {
        bgmSource.clip = gameBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void SetBGMVolume(float value)
    {
        mixer.SetFloat("BGMVolume", LinearToDb(value));
        PlayerPrefs.SetFloat(BGM_KEY, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", LinearToDb(value));
        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= MinLinear) return MutedDb;
        return Mathf.Log10(Mathf.Clamp(linear, MinLinear, 1f)) * 20f;
    }

    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat(BGM_KEY, 1f);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_KEY, 1f);
    }

    private void LoadVolume()
    {
        SetBGMVolume(GetBGMVolume());
        SetSFXVolume(GetSFXVolume());
    }
}
