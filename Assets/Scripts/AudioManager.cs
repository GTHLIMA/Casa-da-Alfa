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
    public AudioClip bombExplosion;
    public AudioClip bombFall;
    public AudioClip warning;
    public AudioClip touchImage;
    public AudioClip groundFall;
    public AudioClip end1;
    public AudioClip end2;
    public AudioClip end3;
    public AudioClip VoiceRecognition;

    [Header("------------- Level 1 sounds -------------")]
    public AudioClip house;
    public AudioClip map;
    public AudioClip dice;
    public AudioClip vase;
    public AudioClip bag;
    public AudioClip ballonPop;

    [Header("------------- Configurações -------------")]
    public float normalPitch = 1f;
    public float speedUpPitch = 1.5f;

    private float savedTime;

    private void Start()
    {
        audioSource.pitch = normalPitch;
        audioSource.loop = true;


        SetSFXVolume(0.2f);
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

    public void SetPitch(float pitch)
    {
        audioSource.pitch = pitch;
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
}