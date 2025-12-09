using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do jogo do trem.
/// ATUALIZAÇÃO: O microfone agora fica INVISÍVEL quando não está em uso.
/// </summary>
public class TrainWhisperGameManager : MonoBehaviour
{
    #region Data Structures
    [System.Serializable]
    public class SyllableData
    {
        public string word;
        public Sprite image;
        public AudioClip hintBasicAudio;      
        public AudioClip hintFinalAudio;      
    }

    [System.Serializable]
    public class VowelDataGroup
    {
        public string groupName;
        public List<SyllableData> syllables;
    }
    #endregion

    #region Public Variables
    [Header("🎯 Configuração Central da Atividade")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;

    [Header("🎤 Reconhecimento de Voz - WHISPER")]
    public WhisperVoiceRecognition whisperVoice;
    public int maxAttempts = 3;

    [Header("🎨 Interface do Microfone")]
    [Tooltip("Imagem do indicador de microfone")]
    public Image micIndicatorImage;
    
    [Tooltip("Sprite quando está GRAVANDO (Verde)")]
    public Sprite micOnSprite; 

    [Tooltip("Sprite quando está dando DICA DE ERRO (Cinza/Vermelho)")]
    public Sprite micOffSprite;

    [Tooltip("Animator do microfone (para pulsar)")]
    public Animator micIndicatorAnimator;

    [Header("🎨 Cores de Feedback")]
    public Color promptingColor = Color.red;      
    public Color normalColor = Color.white;       

    [Header("⚠️ Compatibilidade")]
    [HideInInspector] public Color listeningColor = Color.green; 
    [HideInInspector] public Color staticColor = Color.white;
    [HideInInspector] public List<AudioClip> supportAudios;

    [Header("🔊 Áudios")]
    public AudioClip standardPrompt;
    public List<AudioClip> variablePrompts;
    public AudioClip congratulatoryAudio;

    [Header("🎉 Efeitos")]
    public ParticleSystem endOfLevelConfetti;

    [Header("⏱️ Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float delayAfterHint = 1.5f;
    public float delayAfterPromptBeforeReveal = 0.5f;

    [Header("🚂 Trem")]
    public TrainController trainController;

    [Header("📊 UI")]
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;
    #endregion

    #region Private Variables
    private List<SyllableData> currentSyllableList;
    private int currentIndex = 0;
    private int mistakeCount = 0;
    private bool isProcessing = false;
    private AudioManager audioManager;
    private int score;
    private VoiceGameLogger logger;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Time.timeScale = 1f;
        audioManager = FindObjectOfType<AudioManager>();
    }

    private void Start()
    {
        logger = FindObjectOfType<VoiceGameLogger>();

        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        // Inicia invisível
        SetMicVisuals(false); 
        StartCoroutine(GameLoop());
    }

    private bool ValidateReferences()
    {
        bool valid = true;
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay) valid = false;
        if (whisperVoice == null) valid = false;
        if (micIndicatorImage == null) valid = false;
        if (trainController == null) valid = false;
        
        if (!valid) Debug.LogError("[TrainWhisper] ❌ Referências faltando!");
        return valid;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (isProcessing && Input.GetKeyDown(KeyCode.C)) OnVoiceResult(true);
        if (isProcessing && Input.GetKeyDown(KeyCode.X)) OnVoiceResult(false);
#endif
    }
    #endregion

    #region Game Flow
    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        if (trainController != null)
        {
            yield return StartCoroutine(trainController.AnimateIn(GetCurrentPromptAudio()));
        }

        for (currentIndex = 0; currentIndex < currentSyllableList.Count; currentIndex++)
        {
            yield return StartCoroutine(PlayTurnRoutineForCurrentIndex());

            if (trainController != null) trainController.MarkWagonAsCompleted(currentIndex);

            mistakeCount = 0;

            if (currentIndex < currentSyllableList.Count - 1 && trainController != null)
            {
                yield return StartCoroutine(trainController.AdvanceToNextWagon(currentIndex + 1, GetCurrentPromptAudio(currentIndex + 1)));
            }
        }

        ShowEndPhasePanel();
    }

    private IEnumerator PlayTurnRoutineForCurrentIndex()
    {
        isProcessing = true;
        mistakeCount = 0;

        yield return new WaitForSeconds(delayAfterPromptBeforeReveal);

        if (trainController != null)
        {
            Sprite currentSprite = currentSyllableList[currentIndex].image;
            yield return StartCoroutine(trainController.RevealCurrentImage(currentSprite));
        }

        while (mistakeCount < maxAttempts)
        {
            // Feedback de Erro (Dica)
            if (mistakeCount > 0)
            {
                AudioClip hintClip = GetHintAudioForMistakeCount();
                if (audioManager != null && hintClip != null)
                {
                    // Mostra o microfone rapidinho (vermelho) pra indicar atenção
                    SetMicVisuals(false, true); 
                    
                    audioManager.PlaySFX(hintClip);
                    yield return new WaitForSeconds(hintClip.length + delayAfterHint);
                    
                    // Esconde de novo antes de ativar pra valer
                    SetMicVisuals(false);
                }
            }

            // --- ATIVA O MICROFONE (APARECE NA TELA) ---
            SetMicVisuals(true); 
            Debug.Log($"[TrainWhisper] 🎤🟢 Microfone Visível e Ouvindo");

            string expectedWord = currentSyllableList[currentIndex].word;
            bool voiceResultReceived = false;
            bool wasCorrect = false;

            whisperVoice.StartListening(expectedWord, (result) =>
            {
                voiceResultReceived = true;
                wasCorrect = result;
            });

            float timeout = 0f;
            float maxWaitTime = whisperVoice.maxRecordingTime + 10f; 

            while (!voiceResultReceived && timeout < maxWaitTime)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            // --- DESATIVA O MICROFONE (FICA INVISÍVEL) ---
            SetMicVisuals(false);
            Debug.Log("[TrainWhisper] 🎤⚪ Microfone Invisível");

            if (!voiceResultReceived)
            {
                whisperVoice.StopListening();
                mistakeCount++;
                continue;
            }

            if (wasCorrect)
            {
                AddScore(10);
                if (audioManager != null && congratulatoryAudio != null)
                    audioManager.PlaySFX(congratulatoryAudio);
                yield return new WaitForSeconds(delayAfterCorrect);
                break; 
            }

            mistakeCount++;
            if (mistakeCount >= maxAttempts) break;
        }

        isProcessing = false;
    }
    #endregion

    #region Voice Logic
    private void OnVoiceResult(bool correct) { }
    
    private AudioClip GetCurrentPromptAudio(int specificIndex = -1)
    {
        int indexToUse = (specificIndex == -1) ? currentIndex : specificIndex;
        if (indexToUse < 0 || indexToUse >= currentSyllableList.Count) return null;
        if (mistakeCount == 0 && variablePrompts != null && variablePrompts.Count > 0)
            return variablePrompts[Random.Range(0, variablePrompts.Count)];
        return standardPrompt;
    }

    private AudioClip GetHintAudioForMistakeCount()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];
        switch (mistakeCount)
        {
            case 1: return currentSyllable.hintBasicAudio;
            case 2: return currentSyllable.hintFinalAudio;
            default: return null;
        }
    }
    #endregion

    #region Microphone Visuals (MODIFICADO)
    /// <summary>
    /// Controla a visibilidade e aparência do microfone.
    /// </summary>
    private void SetMicVisuals(bool isActive, bool isPrompting = false)
    {
        if (micIndicatorImage == null) return;

        // LÓGICA: Só aparece se estiver Gravando (active) OU Dando Dica (prompting)
        bool shouldBeVisible = isActive || isPrompting;

        // Liga ou desliga a imagem (Invisível/Visível)
        micIndicatorImage.enabled = shouldBeVisible;

        if (!shouldBeVisible) 
        {
            // Se ficou invisível, desliga animação e sai
            if (micIndicatorAnimator != null) micIndicatorAnimator.SetBool("DevePulsar", false);
            return; 
        }

        // Se chegou aqui, é porque está visível. Vamos configurar a aparência:
        if (isActive)
        {
            // Gravando: Verde e Pulsando
            if (micOnSprite != null) micIndicatorImage.sprite = micOnSprite;
            micIndicatorImage.color = Color.white; 
        }
        else if (isPrompting)
        {
            // Dica de Erro: Sprite Off (ou On) pintado de Vermelho
            if (micOffSprite != null) micIndicatorImage.sprite = micOffSprite;
            micIndicatorImage.color = promptingColor;
        }

        if (micIndicatorAnimator != null)
        {
            micIndicatorAnimator.SetBool("DevePulsar", isActive || isPrompting);
        }
    }
    #endregion

    #region UI & Helpers
    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score;
        if (PauseMenu != null) PauseMenu.SetActive(true);
        Time.timeScale = 0;
    }
    public void ClosePauseMenu()
    {
        if (PauseMenu != null) PauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }
    public void ShowEndPhasePanel()
    {
        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        if (audioManager != null) audioManager.PlaySFX(audioManager.end3);
        if (endOfLevelConfetti != null) endOfLevelConfetti.Play();
        UpdateAllScoreDisplays();
    }
    public void AddScore(int amount)
    {
        score = Mathf.Max(0, score + amount);
        if (numberCounter != null) numberCounter.Value = score;
        ScoreTransfer.Instance?.SetScore(score);
        UpdateAllScoreDisplays();
    }
    private void UpdateAllScoreDisplays()
    {
        string s = score.ToString("000");
        if (scoreHUD) scoreHUD.text = s;
        if (scorePause) scorePause.text = "Score: " + s;
        if (scoreEndPhase) scoreEndPhase.text = s;
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;
        LoadScenes.LoadSceneByIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
    public void GoToMainMenu(int idx = 0)
    {
        Time.timeScale = 1f;
        LoadScenes.LoadSceneByIndex(idx);
    }
    private void OnDestroy() { if (whisperVoice) whisperVoice.StopListening(); }
    #endregion
}