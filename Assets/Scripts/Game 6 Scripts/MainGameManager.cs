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
    
    [Header("=== SPRITE DENTRO DO BALÃO ===")]
    [Tooltip("Sprite que aparece DENTRO do balão e que vai voar para o arco")]
    public Sprite balloonSyllableSprite;
    
    [Header("=== SPRITE DA TELA DE INTRODUÇÃO (INÍCIO) ===")]
    [Tooltip("Sprite que aparece APENAS na primeira tela de apresentação")]
    public Sprite introSprite;
    [Tooltip("☑️ Inverter intro sprite horizontalmente")]
    public bool flipIntroSprite = false;

    [Header("=== SPRITE DA FASE DE VOZ (FINAL) ===")]
    [Tooltip("Sprite que aparece quando o personagem pede para falar (pode ser diferente da intro)")]
    public Sprite voicePhaseSprite; 
    [Tooltip("☑️ Inverter sprite da fase de voz horizontalmente")]
    public bool flipVoicePhaseSprite = false;
    
    [Header("=== SPRITE DO ARCO ===")]
    [Tooltip("Sprite pequeno que fica no centro do hud de progresso")]
    public Sprite arcSprite;
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
    public AudioClip[] voicePromptClips;
    public Image microphoneIcon;
    public Sprite microphoneOffSprite;
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

    [Header("Animation Settings")]
    [Tooltip("Velocidade da sílaba voando do balão para o arco")]
    public float flyingSyllableDuration = 0.6f;

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
        }
        catch (System.Exception e) { Debug.LogWarning(e.Message); }
#endif
    }

    private void Start()
    {
        gameLogger = FindObjectOfType<BalloonVoiceGameLogger>();

        if (balloonManager != null)
            balloonManager.onBalloonPoppedWithPosition += OnBalloonPoppedWithPosition;

        if (musicSource != null) musicSource.Play();
        if (!ValidateReferences()) return;

        if (arcController != null && arcController.centerSyllableImage != null)
        {
            var c = arcController.centerSyllableImage.color;
            c.a = 0f;
            arcController.centerSyllableImage.color = c;
        }

        HideMicrophone();
        if (pauseMenu) pauseMenu.SetActive(false);
        if (endGamePanel) endGamePanel.SetActive(false);

        ShowCurrentSyllableAtCenter();
    }

    void HideMicrophone()
    {
        if (microphoneIcon != null) microphoneIcon.gameObject.SetActive(false);
    }

    private bool ValidateReferences()
    {
        if (!balloonManager || !arcController || syllables.Count == 0 || !introPanelGroup || !syllableIntroImage)
        {
            Debug.LogError("[MainGameManager] Referências críticas faltando!");
            return false;
        }
        return true;
    }

    // --- LÓGICA DE JOGO ---

    void ShowCurrentSyllableAtCenter()
    {
        if (currentSyllableIndex >= syllables.Count)
        {
            EndGame();
            return;
        }

        var data = syllables[currentSyllableIndex];
        if (gameLogger) gameLogger.OnSyllableStarted(data.syllableText, currentSyllableIndex);

        // USANDO SPRITE DE INTRODUÇÃO (INÍCIO)
        if (syllableIntroImage != null)
        {
            syllableIntroImage.sprite = data.introSprite; // <--- AGORA USA O introSprite
            ApplyFlip(syllableIntroImage, data.flipIntroSprite);
        }

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

        if (mainCanvas && syllableStartPosition && syllableArcPosition)
            yield return StartCoroutine(MoveSyllableToArc(data.arcSprite));

        arcController.SetSyllable(data.arcSprite);
        arcController.ResetArc();
        balloonsPopped = 0;

        if (isFirstRound)
        {
            yield return StartCoroutine(FadeInArcSyllable());
            isFirstRound = false;
        }

        balloonManager.onBalloonPopped -= OnBalloonPopped;
        balloonManager.onBalloonPopped += OnBalloonPopped;

        HideMicrophone();
        balloonManager.StartSpawning(data);
        spawningActive = true;
    }

    // --- NOVA LÓGICA DE ANIMAÇÃO DE ESTOURO ---

    // Recebe a posição MUNDIAL do balão (World Position)
    void OnBalloonPoppedWithPosition(Vector2 worldPosition)
    {
        // Chama a corrotina de animação passando a posição
        StartCoroutine(AnimateFloatingSyllable(worldPosition));
    }

    IEnumerator AnimateFloatingSyllable(Vector2 worldPos)
    {
        // 1. Cria um objeto temporário no Canvas
        GameObject flyingObj = new GameObject("FlyingSyllable");
        Image img = flyingObj.AddComponent<Image>();
        
        // Pega o sprite da sílaba atual (que estava dentro do balão)
        Sprite spriteToFly = syllables[currentSyllableIndex].balloonSyllableSprite;
        img.sprite = spriteToFly;
        img.preserveAspect = true;

        // Configura o RectTransform
        RectTransform rt = flyingObj.GetComponent<RectTransform>();
        rt.SetParent(mainCanvas.transform, false);
        
        // 2. Converte Posição do Mundo (Balão) para Posição da Tela (UI)
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        
        // Converte Screen Point para Local Point dentro do Canvas (importante para diferentes resoluções)
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(), 
            screenPoint, 
            mainCanvas.worldCamera, // Se o Canvas for Screen Space - Overlay, pode passar null aqui
            out localPoint
        );
        rt.anchoredPosition = localPoint;
        rt.localScale = Vector3.one; // Começa tamanho normal

        // 3. Define o destino (O ícone do Arco)
        // Precisamos da posição do ArcFillImage ou CenterSyllableImage no Canvas
        RectTransform targetRT = arcController.centerSyllableImage.rectTransform;
        
        // Animação
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        
        // Para pegar a posição ancorada de destino corretamente, as vezes é chato por causa de hierarquias
        // Vamos usar position global do UI e converter de volta, é mais seguro
        Vector3 targetWorldPos = targetRT.position; 
        Vector2 targetLocalPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            Camera.main.WorldToScreenPoint(targetWorldPos),
            mainCanvas.worldCamera,
            out targetLocalPoint
        );

        while (elapsed < flyingSyllableDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyingSyllableDuration;
            
            // Curva suave (Ease In Out)
            t = t * t * (3f - 2f * t);

            // Move
            rt.anchoredPosition = Vector2.Lerp(startPos, targetLocalPoint, t);
            
            // Diminui (Scale de 1.0 para 0.3)
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, t);

            yield return null;
        }

        // Destroi o objeto voador
        Destroy(flyingObj);

        // 4. SÓ AGORA incrementa o progresso
        ProcessPopLogic();
    }

    // Lógica que antes estava no OnBalloonPoppedWithPosition, agora isolada
    void ProcessPopLogic()
    {
        balloonsPopped++;
        arcController.IncrementProgress();

        if (gameLogger) gameLogger.OnBalloonPopped(Vector2.zero); // Log genérico

        Debug.Log($"[MainGameManager] Progresso: {balloonsPopped}/{popsToComplete}");

        HideMicrophone();

        if (balloonsPopped >= popsToComplete && !inVoicePhase)
        {
            spawningActive = false;
            StartCoroutine(BeginVoicePhase());
        }
    }

    void OnBalloonPopped() { } // Não usado diretamente mais, a lógica está no WithPosition

    // --- FASE DE VOZ ---

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
        
        // USANDO SPRITE DA FASE DE VOZ (FINAL) - Alterado aqui!
        if (syllableIntroImage != null)
        {
            // Se tiver sprite específico, usa ele. Se não, usa o introSprite como fallback.
            Sprite spriteToUse = (data.voicePhaseSprite != null) ? data.voicePhaseSprite : data.introSprite;
            syllableIntroImage.sprite = spriteToUse;

            // Usa a flag de flip específica da fase de voz
            bool flipToUse = (data.voicePhaseSprite != null) ? data.flipVoicePhaseSprite : data.flipIntroSprite;
            ApplyFlip(syllableIntroImage, flipToUse);
        }

        if (syllableSource && data.syllableClip)
            syllableSource.PlayOneShot(data.syllableClip);

        yield return new WaitForSeconds(1.5f);

        // Toca pergunta aleatória
        if (voicePromptClips != null && voicePromptClips.Length > 0)
        {
            int r = Random.Range(0, voicePromptClips.Length);
            if (syllableSource)
            {
                syllableSource.PlayOneShot(voicePromptClips[r]);
                yield return new WaitForSeconds(voicePromptClips[r].length + 0.3f);
            }
        }

        SetMicrophoneState(true);
        yield return new WaitForSeconds(0.5f);

        if (voiceManager) voiceManager.StartListening(data.syllableText, OnVoiceResult);
        else OnVoiceResult(false); // Bypass debug
    }

    // Helper para flip
    void ApplyFlip(Image img, bool flip)
    {
        Vector3 s = img.rectTransform.localScale;
        s.x = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        img.rectTransform.localScale = s;
    }

    // --- MANIPULAÇÃO DE CORROTINAS E UI AUXILIAR ---

    IEnumerator MoveSyllableToArc(Sprite sprite)
    {
        GameObject temp = new GameObject("MovingSyllableIntro");
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
        if (temp) Destroy(temp);
    }

    IEnumerator FadeInArcSyllable()
    {
        if (!arcController.centerSyllableImage) yield break;
        float dur = 0.8f;
        Color c = arcController.centerSyllableImage.color;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, t / dur);
            arcController.centerSyllableImage.color = c;
            yield return null;
        }
        c.a = 1f;
        arcController.centerSyllableImage.color = c;
    }

    public void SetMicrophoneState(bool isActive)
    {
        if (!microphoneIcon) return;
        microphoneIcon.gameObject.SetActive(true);
        microphoneIcon.sprite = isActive ? microphoneOnSprite : microphoneOffSprite;
    }

    void OnVoiceResult(bool correct)
    {
        if (!inVoicePhase) return;
        if (gameLogger)
        {
            gameLogger.OnVoiceAttempt("recognized", correct);
            gameLogger.OnSyllableCompleted(correct);
        }

        if (correct)
        {
            SetMicrophoneState(false);
            StartCoroutine(FadeOutSyllableAndAdvance());
        }
        else
        {
            SetMicrophoneState(false);
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
        introPanelGroup.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.6f);
        balloonManager.onBalloonPopped -= OnBalloonPopped;
        currentSyllableIndex++;
        
        if (musicSource) musicSource.UnPause();
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
        introPanelGroup.gameObject.SetActive(false);

        balloonsPopped = 0;
        arcController.ResetArc();
        var data = syllables[currentSyllableIndex];
        
        HideMicrophone();
        if (musicSource) musicSource.UnPause();
        balloonManager.StartSpawning(data);
        spawningActive = true;
    }

    void EndGame()
    {
        if (gameLogger) gameLogger.OnGameCompleted(syllables.Count);
        balloonManager.StopSpawning();
        balloonManager.ClearAllBalloons();
        StartCoroutine(ShowEndGamePanel());
    }

    IEnumerator ShowEndGamePanel()
    {
        yield return new WaitForSeconds(0.5f);
        if (scoreEndGameText) scoreEndGameText.text = $"Completou {syllables.Count} sílabas!";
        if (endGamePanel) endGamePanel.SetActive(true);
        if (confettiEffect) confettiEffect.Play();
        if (musicSource) musicSource.Pause();
        if (sfxSource && endGameClip) sfxSource.PlayOneShot(endGameClip);
    }

    // --- MENUS E CONTROLES ---
    public void OpenPauseMenu()
    {
        if (isPaused) return;
        isPaused = true;
        if (scorePauseText) scorePauseText.text = $"Sílaba: {currentSyllableIndex + 1}/{syllables.Count}";
        if (pauseMenu) pauseMenu.SetActive(true);
        if (musicSource) musicSource.Pause();
        Time.timeScale = 0f;
    }

    public void ClosePauseMenu()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (musicSource && !inVoicePhase) musicSource.UnPause();
        if (pauseMenu) pauseMenu.SetActive(false);
    }

    public void RestartGame() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    public void GoToMainMenu(int idx = 0) => UnityEngine.SceneManagement.SceneManager.LoadScene(idx);

    private void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.C)) OnVoiceResult(true);
        if (Input.GetKeyDown(KeyCode.X)) OnVoiceResult(false);
        #endif
    }
}