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
    [Header("------------- Level 1 sounds -------------")]
    public AudioClip house;
    public AudioClip map;
    public AudioClip dice;
    public AudioClip vase;
    public AudioClip bag;
    public AudioClip ballonPop;

    public float normalPitch = 1f;
    public float speedUpPitch = 1.5f;

    private float savedTime;

    private void Start()
    {
        audioSource.pitch = normalPitch;
        audioSource.loop = true;
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

    public void SetPitch(float isSpeedUp)
    {
        audioSource.pitch = isSpeedUp;
    }
    
    public void PauseAudio(AudioClip clip)
    {
        savedTime = audioSource.time; // Para o audio
        audioSource.Stop();
    }

    public void ResumeAudio(AudioClip clip)
    {
        audioSource.time = savedTime; // Retoma de onde parou
        audioSource.Play();
    }


}
