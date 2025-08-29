using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("------------- Audio Source -------------")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource SFXSource;

    [Header("------------- Audio Clip -------------")]
    public AudioClip background;
    public AudioClip end1;
    public AudioClip end2;
    public AudioClip end3;
    public AudioClip VoiceRecognition;

    [Header("------------- Level 1 sounds -------------")]
    public AudioClip ballonPop;

    [Header("------------- Configurações -------------")]
    // --- NOVA VARIÁVEL ADICIONADA AQUI ---
    [Tooltip("Define o volume inicial para os efeitos sonoros (SFX) nesta cena.")]
    [SerializeField] private float initialSFXVolume = 1.0f; // Valor padrão agora é 1 (máximo)


    private float savedTime;

    private void Start()
    {
        audioSource.loop = true;
        // --- LINHA MODIFICADA ---
        // Agora usa a variável que você pode configurar no Inspector
        SetSFXVolume(initialSFXVolume);
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    public void PlayAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }


    public void PauseAudio(AudioClip clip)
    {
        savedTime = audioSource.time;
        audioSource.Stop();
    }

    public void ResumeAudio(AudioClip clip)
    {
        audioSource.time = savedTime;
        audioSource.Play();
    }


    public void SetSFXVolume(float volume)
    {
        SFXSource.volume = Mathf.Clamp01(volume);
    }

    public void SetBackgroundVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }
}