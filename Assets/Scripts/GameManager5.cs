using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Necessário para CanvasGroup

public class GameManager5 : MonoBehaviour
{
    // Variável para salvar o tempo de reprodução da música, migrada do AudioManager
    private float savedTime;

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
    public AudioClip confettiSfx;
    
    [Header("--- Efeitos Visuais e UI de Controle ---")]
    [Tooltip("Sistema de Partículas de Confete para ser ativado na vitória/fim de fase.")]
    public ParticleSystem confettiEffect;
    
    // Variáveis de UI e Painel de Controle, conforme o GameManager original
    [Tooltip("Painel da UI de Pausa.")]
    public GameObject PauseMenu;
    [Tooltip("Painel da UI de Fim de Fase.")]
    public GameObject endPhasePanel;
    [Tooltip("O ponto de spawn original (se existir).")]
    public Transform spawnPoint;


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
    
    // --- FUNÇÕES DE ÁUDIO DE PAUSA MIGRARADAS ---
    
    // Pausa o áudio na posição atual e salva o tempo (usa musicSource)
    public void PauseAudio(AudioClip clip)
    {
        if (musicSource != null && musicSource.clip == clip)
        {
            savedTime = musicSource.time;
            musicSource.Stop();
        }
    }

    // Retoma o áudio a partir do tempo salvo (usa musicSource)
    public void ResumeAudio(AudioClip clip)
    {
        if (musicSource != null && musicSource.clip == clip)
        {
            musicSource.time = savedTime;
            musicSource.Play();
        }
    }
    
    // --- FUNÇÕES DE CONTROLE DE PAUSA MIGRARADAS ---

    public void OpenPauseMenuLvl1()
    {
        // NOTE: Atualização de score (REMOVIDA)

        // Ativa o painel de pausa
        if (PauseMenu != null)
        {
            PauseMenu.SetActive(true);

            // Garante que o painel da UI continue recebendo cliques
            CanvasGroup cg = PauseMenu.GetComponent<CanvasGroup>();
            if (cg == null) cg = PauseMenu.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Pausa música e áudio ambiente
        if (musicSource != null && backgroundMusic != null)
            PauseAudio(backgroundMusic);

        // Pausa o tempo do jogo
        Time.timeScale = 0f;

        // Pausa todos os AudioListeners no jogo
        AudioListener.pause = true;

        // NOTE: Salva score se houver sistema de transferência (REMOVIDA)

        Debug.Log("Jogo pausado: tempo parado e painel ativo.");
    }

    public void ClosePauseMenuLvl1()
    {
        // Retoma o tempo do jogo
        Time.timeScale = 1f;

        // Retoma todos os áudios pausados
        AudioListener.pause = false;
        if (musicSource != null && backgroundMusic != null)
            ResumeAudio(backgroundMusic);

        // Desativa o painel de pausa
        if (PauseMenu != null)
            PauseMenu.SetActive(false);

        Debug.Log("Jogo retomado.");
    }
    
    // --- FUNÇÕES DE FIM DE FASE MIGRARADAS ---

    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        // NOTE: Atualização de score (REMOVIDA)

        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        
        // Desativa o ponto de spawn (lógica de balões/spawn)
        if (spawnPoint != null) spawnPoint.gameObject.SetActive(false);
        
        // Ativa o confete
        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("Efeito de confete ativado!");
        }

        // NOTE: CancelInvoke(nameof(SpawnPrefab)) (REMOVIDA)

        if (musicSource != null && sfxSource != null)
        {
            // Pausa a música de fundo
            PauseAudio(backgroundMusic);
            
            // Toca o SFX de fim de fase (confettiSfx/end2)
            if (confettiSfx != null)
            {
                 sfxSource.PlayOneShot(confettiSfx, sfxVolume);
            }
        }

        // NOTE: ScoreTransfer (REMOVIDA)
    }

    // --- FUNÇÃO PARA ATIVAR O CONFETE ---
    public void PlayConfetti()
    {
        if (confettiEffect != null)
        {
            confettiEffect.Play();
        }
    }
    // ------------------------------------

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