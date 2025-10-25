// SUBSTITUA O MainGameManager.cs COMPLETO POR ESTE:

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SyllableDado
{
    public string syllableText;
    public Sprite syllableSprite;
    public AudioClip syllableClip;
    public AudioClip correctClip;
}

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance;

    [Header("References")]
    public BalloonManager balloonManager;
    public ArcProgressController arcController;
    public VoiceRecognitionManager voiceManager;

    [Header("AudioSources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource syllableSource;

    [Header("Syllable data")]
    public List<SyllableDado> syllables = new List<SyllableDado>();
    public int currentSyllableIndex = 0;

    [Header("Syllable Intro UI")]
    public CanvasGroup introPanelGroup;
    public Image syllableIntroImage;

    [Header("UI Positions")]
    public Transform syllableStartPosition;
    public Transform syllableArcPosition;
    public Canvas mainCanvas;

    [Header("Voice Phase UI")]
    [Tooltip("Áudios perguntando para falar a sílaba (escolhe aleatório)")]
    public AudioClip[] voicePromptClips;           // 🔊 NOVOS: áudios de pergunta
    
    [Tooltip("Ícone do microfone (Image no Canvas)")]
    public Image microphoneIcon;                    // 🎤 NOVO: ícone do microfone
    
    [Tooltip("Sprite do microfone DESATIVADO (cinza)")]
    public Sprite microphoneOffSprite;              // 🎤 NOVO: sprite cinza
    
    [Tooltip("Sprite do microfone ATIVADO (verde)")]
    public Sprite microphoneOnSprite;               // 🎤 NOVO: sprite verde

    [Header("UI Panels")]
    public GameObject pauseMenu;                    // 🆕 Painel de pausa
    public GameObject endGamePanel;                 // 🆕 Painel de fim de jogo
    public ParticleSystem confettiEffect;           // 🆕 Efeito de confete
    public Text scorePauseText;                     // 🆕 Score no pause (opcional)
    public Text scoreEndGameText;                   // 🆕 Score no end game (opcional)

    [Header("Audio Clips")]
    public AudioClip endGameClip;                   // 🆕 Som de vitória

    [Header("Gameplay")]
    public int popsToComplete = 5;

    private bool inVoicePhase = false;
    private bool spawningActive = false;
    private bool isFirstRound = true;  // 🆕 Flag para controlar fade in do arco
    private bool isPaused = false;     // 🆕 Flag de pausa
    private int balloonsPopped = 0;    // 🆕 Contador de balões estourados no round atual

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

#if UNITY_ANDROID
        if (!SpeechToText.IsInitialized())
            SpeechToText.Initialize("pt-BR");
#endif
    }

    private void Start()
    {
        if (musicSource != null) musicSource.Play();
        
        // Validar referências críticas
        if (!ValidateReferences())
        {
            Debug.LogError("[MainGameManager] Referências críticas não atribuídas! Verifique o Inspector.");
            enabled = false;
            return;
        }

        // 🆕 Inicializar sílaba do arco invisível (só na primeira rodada)
        if (arcController != null && arcController.centerSyllableImage != null)
        {
            var arcSyllableColor = arcController.centerSyllableImage.color;
            arcSyllableColor.a = 0f; // Totalmente transparente
            arcController.centerSyllableImage.color = arcSyllableColor;
        }

        // 🆕 Esconder microfone no início
        if (microphoneIcon != null)
            microphoneIcon.gameObject.SetActive(false);

        // 🆕 Esconder painéis no início
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        ShowCurrentSyllableAtCenter();
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (balloonManager == null)
        {
            Debug.LogError("[MainGameManager] BalloonManager não atribuído!");
            valid = false;
        }

        if (arcController == null)
        {
            Debug.LogError("[MainGameManager] ArcProgressController não atribuído!");
            valid = false;
        }

        if (syllables == null || syllables.Count == 0)
        {
            Debug.LogError("[MainGameManager] Lista de sílabas vazia!");
            valid = false;
        }

        if (introPanelGroup == null)
        {
            Debug.LogError("[MainGameManager] introPanelGroup não atribuído!");
            valid = false;
        }

        if (syllableIntroImage == null)
        {
            Debug.LogError("[MainGameManager] syllableIntroImage não atribuído!");
            valid = false;
        }

        // Avisos (não bloqueiam)
        if (mainCanvas == null)
            Debug.LogWarning("[MainGameManager] mainCanvas não atribuído - animação de movimento desabilitada");
        
        if (syllableStartPosition == null)
            Debug.LogWarning("[MainGameManager] syllableStartPosition não atribuído");
        
        if (syllableArcPosition == null)
            Debug.LogWarning("[MainGameManager] syllableArcPosition não atribuído");

        return valid;
    }

    void ShowCurrentSyllableAtCenter()
    {
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            return;
        }

        var data = syllables[currentSyllableIndex];

        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.syllableSprite;

        StartCoroutine(ShowIntroSequence(data));
    }

    IEnumerator ShowIntroSequence(SyllableDado data)
    {
        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.2f);

        // Fade out
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        // Animar para o arco (se referências existirem)
        if (mainCanvas != null && syllableStartPosition != null && syllableArcPosition != null)
        {
            yield return StartCoroutine(MoveSyllableToArc(data.syllableSprite));
        }
        else
        {
            Debug.LogWarning("[MainGameManager] Pulando animação de movimento - referências faltando");
        }

        // Setup do arco
        arcController.SetSyllable(data.syllableSprite);
        arcController.ResetArc();

        // 🆕 Fade in da sílaba no arco (só na primeira rodada)
        if (isFirstRound && arcController.centerSyllableImage != null)
        {
            yield return StartCoroutine(FadeInArcSyllable());
            isFirstRound = false; // Não fazer fade in nas próximas rodadas
        }

        // Limpar listener antigo e adicionar novo
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        balloonManager.onBalloonPopped += OnBalloonPopped;

        // Iniciar spawn
        balloonManager.StartSpawning(data.syllableSprite);
        spawningActive = true;
    }

    IEnumerator MoveSyllableToArc(Sprite sprite)
    {
        // Validação já feita antes de chamar
        GameObject temp = new GameObject("MovingSyllable");
        Image img = temp.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        RectTransform rt = temp.GetComponent<RectTransform>();
        rt.SetParent(mainCanvas.transform, false);
        rt.position = syllableStartPosition.position;

        float duration = 0.8f;
        Vector3 start = syllableStartPosition.position;
        Vector3 end = syllableArcPosition.position;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (temp == null) yield break; // Safety check
            rt.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        if (temp != null)
            Destroy(temp);
    }

    // 🆕 Fade in da sílaba no arco (primeira rodada)
    IEnumerator FadeInArcSyllable()
    {
        if (arcController.centerSyllableImage == null) yield break;

        float fadeDuration = 0.8f;
        Color color = arcController.centerSyllableImage.color;
        
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            arcController.centerSyllableImage.color = color;
            yield return null;
        }

        color.a = 1f;
        arcController.centerSyllableImage.color = color;
    }

    void OnBalloonPopped()
    {
        arcController.IncrementProgress();

        if (arcController.IsComplete() && !inVoicePhase)
        {
            spawningActive = false;
            StartCoroutine(BeginVoicePhase());
        }
    }

    IEnumerator BeginVoicePhase()
    {
        inVoicePhase = true;

        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();

        if (musicSource) musicSource.Pause();

        yield return new WaitForSeconds(0.5f);

        // Mostrar sílaba e DEIXAR NA TELA até acertar
        var data = syllables[currentSyllableIndex];
        
        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);
        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.syllableSprite;

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.5f);

        // 🆕 TOCAR ÁUDIO DE PERGUNTA (aleatório)
        if (voicePromptClips != null && voicePromptClips.Length > 0)
        {
            int randomIndex = Random.Range(0, voicePromptClips.Length);
            AudioClip promptClip = voicePromptClips[randomIndex];
            
            if (promptClip != null && syllableSource != null)
            {
                Debug.Log($"[MainGameManager] Tocando pergunta {randomIndex + 1}/{voicePromptClips.Length}");
                syllableSource.PlayOneShot(promptClip);
                
                // Aguardar o áudio terminar
                yield return new WaitForSeconds(promptClip.length + 0.3f);
            }
        }

        // 🆕 MOSTRAR MICROFONE CINZA (desativado)
        SetMicrophoneState(false);
        yield return new WaitForSeconds(0.5f);

        // 🆕 ATIVAR MICROFONE (verde)
        SetMicrophoneState(true);

        // Iniciar reconhecimento (sílaba continua visível)
        if (voiceManager != null)
        {
            Debug.Log($"[MainGameManager] Iniciando reconhecimento para: {data.syllableText}");
            voiceManager.StartListening(data.syllableText, OnVoiceResult);
        }
        else
        {
            Debug.LogError("[MainGameManager] VoiceManager não atribuído!");
            OnVoiceResult(false);
        }
    }

    // 🆕 Controla o estado visual do microfone
    void SetMicrophoneState(bool isActive)
    {
        if (microphoneIcon == null) return;

        microphoneIcon.gameObject.SetActive(true);

        if (isActive && microphoneOnSprite != null)
        {
            microphoneIcon.sprite = microphoneOnSprite;
            Debug.Log("[MainGameManager] 🎤 Microfone ATIVADO (verde)");
        }
        else if (!isActive && microphoneOffSprite != null)
        {
            microphoneIcon.sprite = microphoneOffSprite;
            Debug.Log("[MainGameManager] 🎤 Microfone DESATIVADO (cinza)");
        }
    }

    void OnVoiceResult(bool correct)
    {
        if (!inVoicePhase) return;

        // 🆕 Desativar microfone visualmente
        if (microphoneIcon != null)
            microphoneIcon.gameObject.SetActive(false);

        if (correct)
        {
            Debug.Log("[MainGameManager] ✓ Resposta correta!");
            
            // Fade out da sílaba AGORA que acertou
            StartCoroutine(FadeOutSyllableAndAdvance());
        }
        else
        {
            Debug.Log("[MainGameManager] ✗ Resposta incorreta - sílaba continua na tela");
            // Sílaba continua visível, apenas toca o áudio novamente como hint
            var data = syllables[currentSyllableIndex];
            if (syllableSource && data.syllableClip)
                syllableSource.PlayOneShot(data.syllableClip);
        }
    }

    IEnumerator FadeOutSyllableAndAdvance()
    {
        inVoicePhase = false;

        // Tocar som de acerto
        if (sfxSource && syllables[currentSyllableIndex].correctClip)
            sfxSource.PlayOneShot(syllables[currentSyllableIndex].correctClip);

        yield return new WaitForSeconds(0.5f);

        // Fade out da sílaba
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.6f);

        // Avançar para próxima sílaba
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        
        currentSyllableIndex++;
        
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            yield break;
        }

        if (musicSource != null) musicSource.UnPause();
        
        ShowCurrentSyllableAtCenter();
    }

    IEnumerator RestartSameSyllable()
    {
        yield return new WaitForSeconds(1f);
        
        arcController.ResetArc();
        
        if (musicSource != null) musicSource.UnPause();
        
        // Fade out da sílaba
        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);
        
        ShowCurrentSyllableAtCenter();
    }

    IEnumerator AdvanceToNextSyllable(float delay)
    {
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        
        yield return new WaitForSeconds(delay);

        currentSyllableIndex++;
        
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            yield break;
        }

        if (musicSource != null) musicSource.UnPause();
        
        ShowCurrentSyllableAtCenter();
    }

    void EndGame()
    {
        Debug.Log("🎉 Jogo concluído!");
        
        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();
        
        StartCoroutine(ShowEndGamePanel());
    }

    // ========== 🆕 SISTEMA DE PAUSE ==========
    public void OpenPauseMenu()
    {
        if (isPaused) return;

        isPaused = true;

        // Atualiza score no painel (se existir)
        if (scorePauseText != null)
            scorePauseText.text = $"Sílaba: {currentSyllableIndex + 1}/{syllables.Count}";

        // Ativa o painel de pausa
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);

            // Garante que o painel receba cliques
            CanvasGroup cg = pauseMenu.GetComponent<CanvasGroup>();
            if (cg == null) cg = pauseMenu.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Pausa TODAS as músicas
        if (musicSource != null) musicSource.Pause();
        if (sfxSource != null) sfxSource.Pause();
        if (syllableSource != null) syllableSource.Pause();

        // Pausa o tempo do jogo
        Time.timeScale = 0f;

        // Pausa todos os áudios do sistema
        AudioListener.pause = true;

        Debug.Log("🔇 Jogo pausado: tempo parado e áudios pausados.");
    }

    public void ClosePauseMenu()
    {
        if (!isPaused) return;

        isPaused = false;

        // Retoma o tempo do jogo
        Time.timeScale = 1f;

        // Retoma todos os áudios
        AudioListener.pause = false;

        if (musicSource != null && !inVoicePhase) musicSource.UnPause();
        if (sfxSource != null) sfxSource.UnPause();
        if (syllableSource != null) syllableSource.UnPause();

        // Desativa o painel de pausa
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Debug.Log("▶️ Jogo retomado.");
    }

    // ========== 🆕 SISTEMA DE END GAME ==========
    IEnumerator ShowEndGamePanel()
    {
        yield return new WaitForSeconds(0.5f);

        // Atualiza score final (se existir)
        if (scoreEndGameText != null)
            scoreEndGameText.text = $"Completou {syllables.Count} sílabas!";

        // Ativa painel de vitória
        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        // 🎊 ATIVA CONFETE
        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("🎊 Efeito de confete ativado!");
        }

        // Para música de fundo e toca som de vitória
        if (musicSource != null)
            musicSource.Pause();

        if (sfxSource != null && endGameClip != null)
        {
            sfxSource.PlayOneShot(endGameClip);
        }

        Debug.Log("🏆 Painel de vitória exibido!");
    }

    // ========== 🆕 SISTEMA DE RESTART ==========
    public void RestartGame()
    {
        Debug.Log("🔄 Reiniciando jogo...");

        // Retoma tempo ANTES de recarregar cena
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Recarrega a cena atual (reset completo)
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        LoadScenes.LoadSceneByIndex(currentSceneIndex);
    }

    // Alternativa: Reiniciar sem recarregar cena (para testes rápidos)
    public void RestartGameInPlace()
    {
        Debug.Log("🔄 Reiniciando jogo (sem recarregar cena)...");

        // Para todas as coroutines
        StopAllCoroutines();

        // Limpa balões
        if (balloonManager != null)
        {
            balloonManager.StopSpawning();
            balloonManager.ClearAllBalloons();
        }

        // Para voice manager
        if (voiceManager != null)
            voiceManager.StopListening();

        // Reseta flags
        currentSyllableIndex = 0;
        inVoicePhase = false;
        spawningActive = false;
        isFirstRound = true;
        isPaused = false;

        // Retoma tempo
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Esconde painéis
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        if (endGamePanel != null)
            endGamePanel.SetActive(false);
        
        if (introPanelGroup != null)
            introPanelGroup.gameObject.SetActive(false);
        
        if (microphoneIcon != null)
            microphoneIcon.gameObject.SetActive(false);

        // Para confete
        if (confettiEffect != null && confettiEffect.isPlaying)
            confettiEffect.Stop();

        // Reseta arco
        if (arcController != null)
        {
            arcController.ResetArc();
            
            // Reseta transparência da sílaba no arco
            if (arcController.centerSyllableImage != null)
            {
                var color = arcController.centerSyllableImage.color;
                color.a = 0f;
                arcController.centerSyllableImage.color = color;
            }
        }

        // Retoma música de fundo
        if (musicSource != null)
        {
            musicSource.UnPause();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        // Reinicia jogo
        ShowCurrentSyllableAtCenter();

        Debug.Log("✅ Jogo reiniciado com sucesso!");
    }

    // 🆕 Voltar ao menu principal
    public void GoToMainMenu(int menuSceneIndex = 0)
    {
        Debug.Log("🏠 Voltando ao menu principal...");
        
        // Retoma tempo antes de trocar cena
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        LoadScenes.LoadSceneByIndex(menuSceneIndex);
    }

    // ========== DEBUG HOTKEYS (Editor apenas) ==========
    private void Update()
    {
#if UNITY_EDITOR
        // Simular toque mobile com mouse no Editor
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast para detectar cliques em balões mesmo no Editor
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            
            if (hit.collider != null)
            {
                var clickable = hit.collider.GetComponent<BalloonClickable>();
                if (clickable != null)
                    clickable.HandleClick();
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[DEBUG] Simulando voz CORRETA");
            if (inVoicePhase && voiceManager != null)
            {
                voiceManager.StopListening();
                OnVoiceResult(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[DEBUG] Simulando voz INCORRETA");
            if (inVoicePhase && voiceManager != null)
            {
                voiceManager.StopListening();
                OnVoiceResult(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[DEBUG] Toggle Spawn");
            if (spawningActive)
            {
                balloonManager.StopSpawning();
                spawningActive = false;
            }
            else
            {
                if (syllables.Count > 0)
                {
                    balloonManager.StartSpawning(syllables[currentSyllableIndex].syllableSprite);
                    spawningActive = true;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("[DEBUG] Forçar próxima sílaba");
            if (!inVoicePhase)
            {
                balloonManager.onBalloonPopped -= OnBalloonPopped;
                balloonManager.StopSpawning();
                currentSyllableIndex++;
                if (currentSyllableIndex < syllables.Count)
                {
                    ShowCurrentSyllableAtCenter();
                }
                else
                {
                    EndGame();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[DEBUG] Resetar jogo (recarregar cena)");
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("[DEBUG] Resetar in-place (sem recarregar)");
            RestartGameInPlace();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[DEBUG] Completar arco instantaneamente");
            for (int i = 0; i < 5; i++)
            {
                arcController.IncrementProgress();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[DEBUG] Toggle Pause");
            if (isPaused)
                ClosePauseMenu();
            else
                OpenPauseMenu();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[DEBUG] Forçar End Game");
            EndGame();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("[DEBUG] Voltar ao menu (cena 0)");
            GoToMainMenu(0);
        }
#endif
    }
}