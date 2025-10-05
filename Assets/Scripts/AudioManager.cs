using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("------------- Audio Sources -------------")]
    [SerializeField] private AudioSource musicSource;   // Música de fundo
    [SerializeField] private AudioSource sfxSource;     // Efeitos sonoros
    [SerializeField] private AudioSource voiceSource;   // Voz / fala / sílaba

    [Header("------------- Audio Clips -------------")]
    public AudioClip background;       // Música padrão da cena
    public AudioClip end1;
    public AudioClip end2;
    public AudioClip end3;
    public AudioClip VoiceRecognition;
    public AudioClip ballonPop;

    [Header("------------- Volume Controls -------------")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float voiceVolume = 1.0f;

    [Header("------------- Configurações -------------")]
    [Tooltip("Tocar automaticamente a música de fundo ao iniciar a cena.")]
    public bool playBackgroundOnStart = true;

    [Tooltip("Manter este AudioManager entre cenas.")]
    public bool dontDestroyOnLoad = true;

    private float savedMusicTime;

    public static AudioManager Instance;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            if (dontDestroyOnLoad)
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
        ApplyVolumes();

        if (playBackgroundOnStart && background != null)
        {
            PlayMusic(background, true);
        }
    }

    private void Update()
    {
        // Atualiza volumes dinamicamente
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (voiceSource != null) voiceSource.volume = voiceVolume;
    }

    // =====================================================
    //  NOVA API PADRÃO
    // =====================================================
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayVoice(AudioClip clip)
    {
        if (voiceSource == null || clip == null) return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume;
        voiceSource.Play();
    }

    public void PauseAudio(AudioSource source)
    {
        if (source == null) return;
        if (source.isPlaying)
        {
            savedMusicTime = source.time;
            source.Pause();
        }
    }

    public void ResumeAudio(AudioSource source)
    {
        if (source == null) return;
        source.time = savedMusicTime;
        source.Play();
    }

    // =====================================================
    //  COMPATIBILIDADE RETROATIVA (para scripts antigos)
    // =====================================================

    // Chamada antiga: audioManager.SetBackgroundVolume(float)
    public void SetBackgroundVolume(float volume)
    {
        SetMusicVolume(volume);
    }

    // Chamada antiga: audioManager.PlayAudio(AudioClip)
    public void PlayAudio(AudioClip clip)
    {
        PlayMusic(clip, false);
    }

    // Chamada antiga: audioManager.PauseAudio(AudioClip)
    public void PauseAudio(AudioClip clip)
    {
        PauseAudio(musicSource);
    }

    // Chamada antiga: audioManager.ResumeAudio(AudioClip)
    public void ResumeAudio(AudioClip clip)
    {
        ResumeAudio(musicSource);
    }

    // =====================================================
    //  CONTROLE GLOBAL DE VOLUME
    // =====================================================
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        if (voiceSource != null) voiceSource.volume = voiceVolume;
    }
}
