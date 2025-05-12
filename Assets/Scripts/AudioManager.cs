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
    public AudioClip touchImage;
    public AudioClip groundFall;

    public float normalPitch = 1f;
    public float speedUpPitch = 1.5f;

    private void Start() 
    {
        audioSource.clip = background;
        audioSource.pitch = normalPitch;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    public void SetPitch(float isSpeedUp)
    {
        audioSource.pitch = isSpeedUp;
    }


}
