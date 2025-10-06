using System.Collections;
using UnityEngine;

public class GameManager5 : MonoBehaviour
{
    [Header("Audio Manager Global")]
    public AudioManager audioManager; // se quiser linkar manualmente
    private bool useGlobalAudio = false;

    [Header("Local Audio Sources")]
    public AudioSource syllableSource; // som das sílabas ("BA de...")
    public AudioSource optionSource;   // som das figuras (bala, casa...)
    public AudioSource sfxSource;      // sons de acerto, erro, etc.
    public AudioSource musicSource;    // música de fundo

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip correctSFX;

    [Header("Individual Volumes")]
    [Range(0f, 1f)] public float syllableVolume = 1f;
    [Range(0f, 1f)] public float optionVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    void Awake()
    {
        // tenta pegar o AudioManager global, se houver
        audioManager = FindObjectOfType<AudioManager>();
        useGlobalAudio = audioManager != null;

        ApplyVolumes();
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            if (musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
            else if (useGlobalAudio)
            {
                audioManager.PlayMusic(backgroundMusic, true);
            }
        }
    }

    // ===============================
    // 🔊 CONTROLE DE ÁUDIO
    // ===============================

    public void ApplyVolumes()
    {
        if (syllableSource != null) syllableSource.volume = syllableVolume;
        if (optionSource != null) optionSource.volume = optionVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void PlaySyllable(AudioClip clip)
    {
        if (clip == null) return;

        if (syllableSource != null)
        {
            syllableSource.Stop();
            syllableSource.clip = clip;
            syllableSource.volume = syllableVolume;
            syllableSource.Play();
        }
        else if (useGlobalAudio)
        {
            audioManager.PlayVoice(clip);
        }
    }

    public void PlayOption(AudioClip clip)
    {
        if (clip == null) return;

        if (optionSource != null)
        {
            optionSource.Stop();
            optionSource.clip = clip;
            optionSource.volume = optionVolume;
            optionSource.Play();
        }
        else if (useGlobalAudio)
        {
            audioManager.PlayVoice(clip);
        }
    }

    public void PlayCorrect()
    {
        if (correctSFX == null) return;

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(correctSFX);
        }
        else if (useGlobalAudio)
        {
            audioManager.PlaySFX(correctSFX);
        }
    }

    public bool IsSyllablePlaying()
    {
        return syllableSource != null && syllableSource.isPlaying;
    }

    public bool IsOptionPlaying()
    {
        return optionSource != null && optionSource.isPlaying;
    }

    // ===============================
    // 🎥 TREMER A CÂMERA
    // ===============================
    public void ShakeCamera(float duration = 0.35f, float magnitude = 10f)
    {
        if (Camera.main == null) return;
        StopCoroutine(nameof(ShakeCameraCoroutine));
        StartCoroutine(ShakeCameraCoroutine(Camera.main.transform, duration, magnitude));
    }

    private IEnumerator ShakeCameraCoroutine(Transform target, float duration, float magnitude)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp01(percentComplete);
            float x = (Random.value * 2f - 1f) * magnitude * 0.01f * damper;
            float y = (Random.value * 2f - 1f) * magnitude * 0.01f * damper;
            target.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localPosition = originalPos;
    }
}
