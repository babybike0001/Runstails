using UnityEngine;
using System.Collections.Generic;

public sealed class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Default Audio Clips")]
    public AudioClip defaultButtonClick;
    public AudioClip defaultBackgroundMusic;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;

            SetMusicVolume(0.1f);
            SetSFXVolume(0.3f);

            PlayMusic(defaultBackgroundMusic);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        StopAllAudio();
        
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }
    
    public void StopAllAudio()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
        
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }
}