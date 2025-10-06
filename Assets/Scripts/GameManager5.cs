using System.Collections;
using UnityEngine;

public class GameManager5 : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource syllableSource;
    public AudioSource optionSource;
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Individual Volumes")]
    [Range(0f, 1f)] public float syllableVolume = 1f;
    [Range(0f, 1f)] public float optionVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    [Header("SFX Clips")]
    public AudioClip correctSfx;
    public AudioClip wrongSfx;

    void Start()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    // Toca o som da sílaba (pergunta)
    public void PlaySyllable(AudioClip clip)
    {
        if (syllableSource == null || clip == null) return;
        syllableSource.Stop();
        syllableSource.volume = syllableVolume;
        syllableSource.clip = clip;
        syllableSource.Play();
    }

    // Toca o som do desenho clicado (option)
    public void PlayOption(AudioClip clip)
    {
        if (optionSource == null || clip == null) return;
        optionSource.Stop();
        optionSource.volume = optionVolume;
        optionSource.clip = clip;
        optionSource.Play();
    }

    // Som de acerto
    public void PlayCorrect()
    {
        if (sfxSource == null) return;
        if (correctSfx != null)
            sfxSource.PlayOneShot(correctSfx, sfxVolume);
    }

    // Som de erro
    public void PlayWrong()
    {
        if (sfxSource == null) return;
        if (wrongSfx != null)
            sfxSource.PlayOneShot(wrongSfx, sfxVolume);
    }

    public bool IsSyllablePlaying() => syllableSource != null && syllableSource.isPlaying;
    public bool IsOptionPlaying() => optionSource != null && optionSource.isPlaying;

    public void PlayMusic()
    {
        if (musicSource == null || backgroundMusic == null) return;
        if (!musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    // Shake simples de câmera
    public void ShakeCamera(float duration = 0.35f, float magnitude = 10f)
    {
        if (Camera.main == null) return;
        StopCoroutine("ShakeCameraCoroutine");
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
