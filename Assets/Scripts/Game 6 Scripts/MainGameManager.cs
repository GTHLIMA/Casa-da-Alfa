using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SyllableDado
{
    [Header("=== TEXTOS E ÁUDIOS ===")]
    public string syllableText;
    public AudioClip syllableClip;
    public AudioClip correctClip;
    
    [Header("=== SPRITE DA SÍLABA NO BALÃO ===")]
    [Tooltip("Sprite da SÍLABA que aparece DENTRO do balão (ex: 'BA', 'CA')")]
    public Sprite balloonSyllableSprite;
    
    [Header("=== SPRITE DO ARCO ===")]
    [Tooltip("Sprite que aparece NO ARCO (pode ser dica visual, imagem, etc)")]
    public Sprite arcHintSprite;
}

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance;

    [Header("References")]
    public BalloonManager balloonManager;
    public ArcProgressController arcController;
    public WhisperVoiceRecognition voiceManager;

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
    public AudioClip[] voicePromptClips;

    [Tooltip("Ícone do microfone (Image no Canvas)")]
    public Image microphoneIcon;

    [Tooltip("Sprite do microfone DESATIVADO (cinza)")]
    public Sprite microphoneOffSprite;

    [Tooltip("Sprite do microfone ATIVADO (verde)")]
    public Sprite microphoneOnSprite;

    [Header("UI Panels")]
    public GameObject pauseMenu;
    public GameObject endGamePanel;
    public ParticleSystem confettiEffect;
    public Text scorePauseText;
    public Text scoreEndGameText;

    [Header("Audio Clips")]
    public AudioClip endGameClip;

    [Header("Gameplay")]
    public int popsToComplete = 5;

    private bool inVoicePhase = false;
    private bool spawningActive = false;
    private bool isFirstRound = true;
    private bool isPaused = false;
    private int balloonsPopped = 0;

    private BalloonVoiceGameLogger gameLogger;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

#if UNITY_ANDROID
        try
        {
            SpeechToText.Initialize("pt-BR");
            Debug.Log("[MainGameManager] SpeechToText inicializado");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainGameManager] Erro ao inicializar STT: {e.Message}");
        }
#endif
    }

    private void Start()
    {
        gameLogger = FindObjectOfType<BalloonVoiceGameLogger>();

        if (balloonManager != null)
        {
            balloonManager.onBalloonPoppedWithPosition += OnBalloonPoppedWithPosition;
        }

        if (musicSource != null) musicSource.Play();

        if (!ValidateReferences())
        {
            Debug.LogError("[MainGameManager] Referências críticas não atribuídas! Verifique o Inspector.");
            enabled = false;
            return;
        }

        if (arcController != null && arcController.centerSyllableImage != null)
        {
            var arcSyllableColor = arcController.centerSyllableImage.color;
            arcSyllableColor.a = 0f;
            arcController.centerSyllableImage.color = arcSyllableColor;
        }

        HideMicrophone();

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        ShowCurrentSyllableAtCenter();
    }

    void HideMicrophone()
    {
        if (microphoneIcon != null)
        {
            microphoneIcon.gameObject.SetActive(false);
            Debug.Log("[MainGameManager] 🎤 Microfone DESATIVADO");
        }
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

        // 🆕 USA O SPRITE DO BALÃO PARA A INTRO
        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.balloonSyllableSprite;

        StartCoroutine(ShowIntroSequence(data));
    }

    IEnumerator ShowIntroSequence(SyllableDado data)
    {
        HideMicrophone();

        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.2f);

        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        if (mainCanvas != null && syllableStartPosition != null && syllableArcPosition != null)
        {
            // 🆕 ANIMA O SPRITE DO ARCO (não o do balão)
            yield return StartCoroutine(MoveSyllableToArc(data.arcHintSprite));
        }
        else
        {
            Debug.LogWarning("[MainGameManager] Pulando animação de movimento - referências faltando");
        }

        // 🆕 ARCO RECEBE O SPRITE DE DICA
        arcController.SetSyllable(data.arcHintSprite);
        arcController.ResetArc();

        balloonsPopped = 0;

        if (isFirstRound && arcController.centerSyllableImage != null)
        {
            yield return StartCoroutine(FadeInArcSyllable());
            isFirstRound = false;
        }

        balloonManager.onBalloonPopped -= OnBalloonPopped;
        balloonManager.onBalloonPopped += OnBalloonPopped;

        HideMicrophone();

        // 🆕 PASSA DADOS COMPLETOS PARA O BALLOON MANAGER
        balloonManager.StartSpawning(data);
        spawningActive = true;
    }

    IEnumerator MoveSyllableToArc(Sprite sprite)
    {
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
            if (temp == null) yield break;
            rt.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        if (temp != null)
            Destroy(temp);
    }

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

    void OnBalloonPoppedWithPosition(Vector2 position)
    {
        balloonsPopped++;
        arcController.IncrementProgress();

        if (gameLogger != null)
        {
            string currentSyllable = syllables[currentSyllableIndex].syllableText;
            
            Vector2 normalizedPosition = new Vector2(
                position.x / Screen.width,
                position.y / Screen.height
            );
            
            gameLogger.LogBalloonPopWithPosition(currentSyllable, currentSyllableIndex, normalizedPosition);
        }

        Debug.Log($"[MainGameManager] Balão estourado na posição: {position} | Total: {balloonsPopped}/{popsToComplete}");

        HideMicrophone();

        if (balloonsPopped >= popsToComplete && !inVoicePhase)
        {
            Debug.Log("[MainGameManager] ✓ Arco completo! Indo para fase de voz...");
            spawningActive = false;
            StartCoroutine(BeginVoicePhase());
        }
    }

    void OnBalloonPopped()
    {
        OnBalloonPoppedWithPosition(Vector2.zero);
    }

    IEnumerator BeginVoicePhase()
    {
        inVoicePhase = true;

        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();

        if (musicSource) musicSource.Pause();

        yield return new WaitForSeconds(0.5f);

        var data = syllables[currentSyllableIndex];

        introPanelGroup.alpha = 1f;
        introPanelGroup.gameObject.SetActive(true);
        
        // 🆕 MOSTRA SPRITE DO BALÃO NA INTRO DE VOZ
        if (syllableIntroImage != null)
            syllableIntroImage.sprite = data.balloonSyllableSprite;

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.5f);

        if (voicePromptClips != null && voicePromptClips.Length > 0)
        {
            int randomIndex = Random.Range(0, voicePromptClips.Length);
            AudioClip promptClip = voicePromptClips[randomIndex];

            if (promptClip != null && syllableSource != null)
            {
                Debug.Log($"[MainGameManager] Tocando pergunta {randomIndex + 1}/{voicePromptClips.Length}");
                syllableSource.PlayOneShot(promptClip);

                yield return new WaitForSeconds(promptClip.length + 0.3f);
            }
        }

        SetMicrophoneState(true);
        yield return new WaitForSeconds(0.5f);

        if (voiceManager != null)
        {
            Debug.Log($"[MainGameManager] 🎤 Iniciando reconhecimento para: {data.syllableText}");
            voiceManager.StartListening(data.syllableText, OnVoiceResult);
        }
        else
        {
            Debug.LogError("[MainGameManager] VoiceManager não atribuído!");
            OnVoiceResult(false);
        }
    }

    public void SetMicrophoneState(bool isActive)
    {
        if (microphoneIcon == null)
        {
            Debug.LogError("[MainGameManager] ❌ microphoneIcon é NULL! Atribua no Inspector!");
            return;
        }

        microphoneIcon.gameObject.SetActive(true);

        if (isActive)
        {
            if (microphoneOnSprite == null)
            {
                Debug.LogError("[MainGameManager] ❌ microphoneOnSprite (verde) é NULL! Atribua no Inspector!");
                return;
            }

            microphoneIcon.sprite = microphoneOnSprite;
            Debug.Log("[MainGameManager] 🎤🟢 Microfone ATIVADO (verde)");
        }
        else
        {
            if (microphoneOffSprite == null)
            {
                Debug.LogError("[MainGameManager] ❌ microphoneOffSprite (cinza) é NULL! Atribua no Inspector!");
                return;
            }

            microphoneIcon.sprite = microphoneOffSprite;
            Debug.Log("[MainGameManager] 🎤⚪ Microfone DESATIVADO (cinza)");
        }
    }

    void OnVoiceResult(bool correct)
    {
        if (!inVoicePhase) return;

        if (gameLogger != null)
        {
            string expectedSyllable = syllables[currentSyllableIndex].syllableText;
            
            gameLogger.LogVoiceAttempt(expectedSyllable, "recognized", currentSyllableIndex, 1, correct);
            
            if (correct)
            {
                gameLogger.LogSyllableCompleted(expectedSyllable, currentSyllableIndex, true, balloonsPopped, 1);
            }
        }

        if (correct)
        {
            SetMicrophoneState(false);
            Debug.Log("[MainGameManager] ✅ Resposta correta! Avançando para próxima sílaba...");
            StartCoroutine(FadeOutSyllableAndAdvance());
        }
        else
        {
            SetMicrophoneState(false);
            Debug.Log("[MainGameManager] ❌ Esgotou 3 tentativas - reinicia round de balões");
            StartCoroutine(RestartSameSyllable());
        }
    }

    IEnumerator FadeOutSyllableAndAdvance()
    {
        inVoicePhase = false;

        if (sfxSource && syllables[currentSyllableIndex].correctClip)
            sfxSource.PlayOneShot(syllables[currentSyllableIndex].correctClip);

        yield return new WaitForSeconds(0.5f);

        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.6f);

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
        inVoicePhase = false;

        yield return new WaitForSeconds(0.5f);

        float fadeTime = 0.6f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            introPanelGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        introPanelGroup.alpha = 0f;
        introPanelGroup.gameObject.SetActive(false);

        balloonsPopped = 0;
        arcController.ResetArc();

        var data = syllables[currentSyllableIndex];
        Debug.Log($"[MainGameManager] ♻️ Voltando para fase de balões da sílaba '{data.syllableText}'");

        HideMicrophone();

        if (musicSource != null) musicSource.UnPause();

        balloonManager.StartSpawning(data);
        spawningActive = true;
    }

    void EndGame()
    {
        if (gameLogger != null)
        {
            int successfulSyllables = currentSyllableIndex;
            int totalBalloons = balloonsPopped;
            int totalVoiceAttempts = successfulSyllables;
            
            gameLogger.LogGameCompleted(syllables.Count, totalBalloons, totalVoiceAttempts, successfulSyllables);
        }

        Debug.Log("🎉 Jogo concluído!");

        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();

        StartCoroutine(ShowEndGamePanel());
    }

    public void OpenPauseMenu()
    {
        if (isPaused) return;

        isPaused = true;

        if (scorePauseText != null)
            scorePauseText.text = $"Sílaba: {currentSyllableIndex + 1}/{syllables.Count}";

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);

            CanvasGroup cg = pauseMenu.GetComponent<CanvasGroup>();
            if (cg == null) cg = pauseMenu.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        if (musicSource != null) musicSource.Pause();
        if (sfxSource != null) sfxSource.Pause();
        if (syllableSource != null) syllableSource.Pause();

        Time.timeScale = 0f;
        AudioListener.pause = true;

        Debug.Log("🔇 Jogo pausado: tempo parado e áudios pausados.");
    }

    public void ClosePauseMenu()
    {
        if (!isPaused) return;

        isPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (musicSource != null && !inVoicePhase) musicSource.UnPause();
        if (sfxSource != null) sfxSource.UnPause();
        if (syllableSource != null) syllableSource.UnPause();

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Debug.Log("▶️ Jogo retomado.");
    }

    IEnumerator ShowEndGamePanel()
    {
        yield return new WaitForSeconds(0.5f);

        if (scoreEndGameText != null)
            scoreEndGameText.text = $"Completou {syllables.Count} sílabas!";

        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("🎊 Efeito de confete ativado!");
        }

        if (musicSource != null)
            musicSource.Pause();

        if (sfxSource != null && endGameClip != null)
        {
            sfxSource.PlayOneShot(endGameClip);
        }

        Debug.Log("🏆 Painel de vitória exibido!");
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Reiniciando jogo...");

        Time.timeScale = 1f;
        AudioListener.pause = false;

        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
    }

    public void RestartGameInPlace()
    {
        Debug.Log("🔄 Reiniciando jogo (sem recarregar cena)...");

        StopAllCoroutines();

        if (balloonManager != null)
        {
            balloonManager.StopSpawning();
            balloonManager.ClearAllBalloons();
        }

        if (voiceManager != null)
            voiceManager.StopListening();

        currentSyllableIndex = 0;
        inVoicePhase = false;
        spawningActive = false;
        isFirstRound = true;
        isPaused = false;
        balloonsPopped = 0;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        if (introPanelGroup != null)
            introPanelGroup.gameObject.SetActive(false);

        HideMicrophone();

        if (confettiEffect != null && confettiEffect.isPlaying)
            confettiEffect.Stop();

        if (arcController != null)
        {
            arcController.ResetArc();
            
            if (arcController.centerSyllableImage != null)
            {
                var color = arcController.centerSyllableImage.color;
                color.a = 0f;
                arcController.centerSyllableImage.color = color;
            }
        }

        if (musicSource != null)
        {
            musicSource.UnPause();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        ShowCurrentSyllableAtCenter();

        Debug.Log("✅ Jogo reiniciado com sucesso!");
    }

    public void GoToMainMenu(int menuSceneIndex = 0)
    {
        Debug.Log("🏠 Voltando ao menu principal...");
        
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneIndex);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
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
                    balloonManager.StartSpawning(syllables[currentSyllableIndex]);
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