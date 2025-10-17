using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager5 : MonoBehaviour
{
    [Header("----- Áudio Principal -----")]
    public AudioSource musicSource;      // Música de fundo
    public AudioSource sfxSource;        // Efeitos sonoros
    public AudioSource syllableSource;   // Voz/sílaba
    public AudioSource optionSource;     // Som das opções

    [Header("----- Clips de Áudio -----")]
    public AudioClip backgroundMusic;    // Música de fundo da cena
    public AudioClip correctSfx;         // Som de acerto
    public AudioClip wrongSfx;           // Som de erro
    public AudioClip confettiSfx;        // Som de finalização

    [Header("----- Controle de Volume -----")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float syllableVolume = 1f;
    [Range(0f, 1f)] public float optionVolume = 1f;

    private static GameManager5 instance;
    private float savedTime;
    private bool isPaused = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureAudioSources();
        ApplyVolumes();
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic, true);
        }
    }

    // =====================================
    // 🔊 SISTEMA DE ÁUDIO COMPLETO
    // =====================================
    private void EnsureAudioSources()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (syllableSource == null) syllableSource = gameObject.AddComponent<AudioSource>();
        if (optionSource == null) optionSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
    }

    public void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (syllableSource != null) syllableSource.volume = syllableVolume;
        if (optionSource != null) optionSource.volume = optionVolume;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            savedTime = musicSource.time;
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        musicSource.time = savedTime;
        musicSource.Play();
    }

    public void PlaySyllable(AudioClip clip)
    {
        if (clip == null) return;
        syllableSource.Stop();
        syllableSource.clip = clip;
        syllableSource.volume = syllableVolume;
        syllableSource.Play();
    }

    public void PlayOption(AudioClip clip)
    {
        if (clip == null) return;
        optionSource.Stop();
        optionSource.clip = clip;
        optionSource.volume = optionVolume;
        optionSource.Play();
    }

    public void PlayCorrect()
    {
        if (correctSfx != null)
            sfxSource.PlayOneShot(correctSfx, sfxVolume);
    }

    public void PlayWrong()
    {
        if (wrongSfx != null)
            sfxSource.PlayOneShot(wrongSfx, sfxVolume);
    }

    public void PlayConfettiSound()
    {
        if (confettiSfx != null)
            sfxSource.PlayOneShot(confettiSfx, sfxVolume);
    }

    public bool IsSyllablePlaying()
    {
        return syllableSource != null && syllableSource.isPlaying;
    }

    public bool IsOptionPlaying()
    {
        return optionSource != null && optionSource.isPlaying;
    }

    // =====================================
    // 💥 CÂMERA / EFEITOS
    // =====================================
    public void ShakeCamera(float duration = 0.3f, float magnitude = 10f)
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
            float x = Random.Range(-1f, 1f) * magnitude * 0.01f;
            float y = Random.Range(-1f, 1f) * magnitude * 0.01f;
            target.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
    }
}