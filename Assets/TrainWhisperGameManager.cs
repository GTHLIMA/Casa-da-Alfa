using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador do jogo do trem usando Whisper para reconhecimento de voz
/// Substitui o ImageVoiceMatcher com integração do WhisperVoiceRecognition
/// </summary>
public class TrainWhisperGameManager : MonoBehaviour
{
    #region Data Structures
    [System.Serializable]
    public class SyllableData
    {
        public string word;
        public Sprite image;
        public AudioClip hintBasicAudio;      // 1ª dica após erro
        public AudioClip hintFinalAudio;      // 2ª dica após erro
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
    [Tooltip("Índice do grupo de vogais/palavras a ser jogado")]
    public int vowelIndexToPlay = 0;
    
    [Tooltip("Todos os grupos de palavras disponíveis")]
    public List<VowelDataGroup> allVowelData;

    [Header("🎤 Reconhecimento de Voz - WHISPER")]
    [Tooltip("Referência ao WhisperVoiceRecognition")]
    public WhisperVoiceRecognition whisperVoice;
    
    [Tooltip("Número máximo de tentativas (2 dicas + 1 tentativa final = 3)")]
    public int maxAttempts = 3;

    [Header("🎨 Interface do Microfone")]
    [Tooltip("Imagem do indicador de microfone")]
    public Image micIndicatorImage;
    
    [Tooltip("Animator do microfone (para pulsar)")]
    public Animator micIndicatorAnimator;

    [Header("🎨 Cores do Indicador")]
    public Color promptingColor = Color.red;      // Quando está tocando pergunta
    public Color listeningColor = Color.green;    // Quando está gravando
    public Color staticColor = Color.white;       // Quando está parado

    [Header("🔊 Áudios de Feedback")]
    [Tooltip("Áudio padrão de pergunta inicial")]
    public AudioClip standardPrompt;
    
    [Tooltip("Lista de perguntas variadas (escolhe aleatório)")]
    public List<AudioClip> variablePrompts;
    
    [Tooltip("Áudio de parabéns quando acerta")]
    public AudioClip congratulatoryAudio;
    
    [Tooltip("Áudios de apoio (não usado ainda, mas mantido para compatibilidade)")]
    public List<AudioClip> supportAudios;

    [Header("🎉 Efeitos Visuais")]
    [Tooltip("Confete ao finalizar todas as palavras")]
    public ParticleSystem endOfLevelConfetti;

    [Header("⏱️ Controles de Tempo")]
    [Tooltip("Delay antes de começar o jogo")]
    public float initialDelay = 2.0f;
    
    [Tooltip("Delay após acertar antes de avançar")]
    public float delayAfterCorrect = 1.0f;
    
    [Tooltip("Delay após tocar dica")]
    public float delayAfterHint = 1.5f;
    
    [Tooltip("Delay após pergunta antes de revelar imagem")]
    public float delayAfterPromptBeforeReveal = 0.5f;

    [Header("🚂 Animações do Trem")]
    [Tooltip("Controlador do trem")]
    public TrainController trainController;

    [Header("📊 UI - Pause Menu & Score")]
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
    private VoiceGameLogger logger; // Firebase
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

        // Carrega score
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();

        // Validações críticas
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        SetMicIndicator(staticColor);
        StartCoroutine(GameLoop());
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || 
            allVowelData[vowelIndexToPlay].syllables.Count == 0)
        {
            Debug.LogError("[TrainWhisper] ❌ 'All Vowel Data' não configurado ou vazio!");
            valid = false;
        }

        if (whisperVoice == null)
        {
            Debug.LogError("[TrainWhisper] ❌ 'WhisperVoiceRecognition' não atribuído! Configure no Inspector.");
            valid = false;
        }

        if (micIndicatorImage == null)
        {
            Debug.LogError("[TrainWhisper] ❌ 'Mic Indicator Image' não atribuído!");
            valid = false;
        }

        if (trainController == null)
        {
            Debug.LogError("[TrainWhisper] ❌ 'Train Controller' não atribuído!");
            valid = false;
        }

        return valid;
    }

    private void Update()
    {
        // Atalhos de debug apenas no Editor
#if UNITY_EDITOR
        if (isProcessing && Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[DEBUG] Simulando acerto com tecla C");
            OnVoiceResult(true);
        }

        if (isProcessing && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[DEBUG] Simulando erro com tecla X");
            OnVoiceResult(false);
        }
#endif
    }
    #endregion

    #region Main Game Flow
    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        if (trainController != null)
        {
            AudioClip firstPrompt = GetCurrentPromptAudio();
            Debug.Log($"[TrainWhisper] 🚂 Trem entrando com pergunta: {(firstPrompt != null ? firstPrompt.name : "null")}");
            yield return StartCoroutine(trainController.AnimateIn(firstPrompt));
        }

        for (currentIndex = 0; currentIndex < currentSyllableList.Count; currentIndex++)
        {
            Debug.Log($"[TrainWhisper] 🎯 Palavra {currentIndex + 1}/{currentSyllableList.Count}: {currentSyllableList[currentIndex].word}");
            
            yield return StartCoroutine(PlayTurnRoutineForCurrentIndex());

            if (trainController != null)
            {
                trainController.MarkWagonAsCompleted(currentIndex);
            }

            mistakeCount = 0;

            if (currentIndex < currentSyllableList.Count - 1 && trainController != null)
            {
                AudioClip nextPrompt = GetCurrentPromptAudio(currentIndex + 1);
                Debug.Log($"[TrainWhisper] ➡️ Avançando para próxima palavra...");
                yield return StartCoroutine(trainController.AdvanceToNextWagon(currentIndex + 1, nextPrompt));
            }
        }

        ShowEndPhasePanel();
    }

    private IEnumerator PlayTurnRoutineForCurrentIndex()
    {
        isProcessing = true;
        mistakeCount = 0;

        // Aguarda antes de revelar a imagem
        yield return new WaitForSeconds(delayAfterPromptBeforeReveal);

        // Revela a imagem da palavra atual
        if (trainController != null)
        {
            Sprite currentSprite = currentSyllableList[currentIndex].image;
            logger?.LogImageProgress(currentSyllableList[currentIndex].word, currentIndex);
            yield return StartCoroutine(trainController.RevealCurrentImage(currentSprite));
        }

        // Loop de tentativas
        while (mistakeCount < maxAttempts)
        {
            // Se não é a primeira tentativa, toca a dica correspondente
            if (mistakeCount > 0)
            {
                AudioClip hintClip = GetHintAudioForMistakeCount();
                if (audioManager != null && hintClip != null)
                {
                    Debug.Log($"[TrainWhisper] 💡 Tocando dica {mistakeCount}/{maxAttempts - 1}: {hintClip.name}");
                    SetMicIndicator(promptingColor);
                    audioManager.PlaySFX(hintClip);
                    logger?.LogHint(currentSyllableList[currentIndex].word, mistakeCount);

                    yield return new WaitForSeconds(hintClip.length + delayAfterHint);
                }
            }

            // Ativa o microfone (verde + pulsar)
            SetMicIndicator(listeningColor, true);
            Debug.Log($"[TrainWhisper] 🎤🟢 Microfone ATIVADO - Tentativa {mistakeCount + 1}/{maxAttempts}");

            // Inicia escuta com Whisper
            string expectedWord = currentSyllableList[currentIndex].word;
            bool voiceResultReceived = false;
            bool wasCorrect = false;

            whisperVoice.StartListening(expectedWord, (result) =>
            {
                voiceResultReceived = true;
                wasCorrect = result;
            });

            // Aguarda resultado do Whisper
            float timeout = 0f;
            float maxWaitTime = whisperVoice.maxRecordingTime + 10f; // Gravação + processamento

            while (!voiceResultReceived && timeout < maxWaitTime)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            // Desativa microfone
            SetMicIndicator(staticColor);
            Debug.Log("[TrainWhisper] 🎤⚪ Microfone DESATIVADO");

            // Timeout
            if (!voiceResultReceived)
            {
                Debug.LogWarning($"[TrainWhisper] ⏱️ Timeout após {maxWaitTime}s sem resposta");
                whisperVoice.StopListening();
                mistakeCount++;
                continue;
            }

            // Acertou!
            if (wasCorrect)
            {
                Debug.Log($"[TrainWhisper] ✅ CORRETO: '{expectedWord}'");
                AddScore(10);
                logger?.LogCorrect(expectedWord);

                if (audioManager != null && congratulatoryAudio != null)
                {
                    audioManager.PlaySFX(congratulatoryAudio);
                }

                yield return new WaitForSeconds(delayAfterCorrect);
                break; // Sai do loop de tentativas
            }

            // Errou
            mistakeCount++;
            Debug.Log($"[TrainWhisper] ❌ INCORRETO - Tentativa {mistakeCount}/{maxAttempts}");
            logger?.LogError(expectedWord, "voz não reconhecida corretamente");

            // Esgotou tentativas
            if (mistakeCount >= maxAttempts)
            {
                Debug.Log($"[TrainWhisper] ⚠️ Esgotou {maxAttempts} tentativas. Avançando para próxima palavra.");
                break;
            }
        }

        isProcessing = false;
    }
    #endregion

    #region Voice Recognition Callback
    private void OnVoiceResult(bool correct)
    {
        // Este método é chamado pelo callback do WhisperVoiceRecognition
        // A lógica está no PlayTurnRoutineForCurrentIndex através do callback lambda
        Debug.Log($"[TrainWhisper] 🎯 Resultado da voz: {(correct ? "✅ Correto" : "❌ Incorreto")}");
    }
    #endregion

    #region Prompts & Hints
    private AudioClip GetCurrentPromptAudio(int specificIndex = -1)
    {
        int indexToUse = (specificIndex == -1) ? currentIndex : specificIndex;
        if (indexToUse < 0 || indexToUse >= currentSyllableList.Count) return null;

        // Primeira tentativa: usa prompt variável ou padrão
        if (mistakeCount == 0)
        {
            if (variablePrompts != null && variablePrompts.Count > 0)
            {
                return variablePrompts[Random.Range(0, variablePrompts.Count)];
            }
            return standardPrompt;
        }

        return null; // Dicas são chamadas por GetHintAudioForMistakeCount
    }

    private AudioClip GetHintAudioForMistakeCount()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];

        switch (mistakeCount)
        {
            case 1:
                return currentSyllable.hintBasicAudio;  // 1ª dica
            case 2:
                return currentSyllable.hintFinalAudio;  // 2ª dica
            default:
                return null;
        }
    }
    #endregion

    #region Microphone Indicator
    private void SetMicIndicator(Color color, bool shouldPulse = false)
    {
        if (micIndicatorImage != null)
        {
            micIndicatorImage.color = color;
        }

        if (micIndicatorAnimator != null)
        {
            micIndicatorAnimator.SetBool("DevePulsar", shouldPulse);
        }
    }
    #endregion

    #region UI & Score Management
    public void OpenPauseMenu()
    {
        if (scorePause != null)
            scorePause.text = "Score: " + score.ToString();

        if (PauseMenu != null)
            PauseMenu.SetActive(true);

        Time.timeScale = 0;
        ScoreTransfer.Instance?.SetScore(score);

        Debug.Log("[TrainWhisper] ⏸️ Jogo pausado");
    }

    public void ClosePauseMenu()
    {
        if (PauseMenu != null)
            PauseMenu.SetActive(false);

        Time.timeScale = 1f;

        Debug.Log("[TrainWhisper] ▶️ Jogo retomado");
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("[TrainWhisper] 🎉 FIM DE JOGO!");

        if (endPhasePanel != null)
            endPhasePanel.SetActive(true);

        if (audioManager != null && audioManager.end3 != null)
            audioManager.PlaySFX(audioManager.end3);

        if (endOfLevelConfetti != null)
            endOfLevelConfetti.Play();

        UpdateAllScoreDisplays();
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;

        if (numberCounter != null)
            numberCounter.Value = score;

        ScoreTransfer.Instance?.SetScore(score);
        UpdateAllScoreDisplays();

        Debug.Log($"[TrainWhisper] 📊 Score: {score} (+{amount})");
    }

    private void UpdateAllScoreDisplays()
    {
        string formattedScore = score.ToString("000");

        if (scoreHUD != null)
            scoreHUD.text = formattedScore;

        if (scorePause != null)
            scorePause.text = "Score: " + formattedScore;

        if (scoreEndPhase != null)
            scoreEndPhase.text = formattedScore;
    }

    public void RestartGame()
    {
        Debug.Log("[TrainWhisper] 🔄 Reiniciando jogo...");
        Time.timeScale = 1f;
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        LoadScenes.LoadSceneByIndex(currentSceneIndex);
    }

    public void GoToMainMenu(int menuSceneIndex = 0)
    {
        Debug.Log("[TrainWhisper] 🏠 Voltando ao menu...");
        Time.timeScale = 1f;
        LoadScenes.LoadSceneByIndex(menuSceneIndex);
    }
    #endregion

    #region Cleanup
    private void OnDestroy()
    {
        if (whisperVoice != null)
        {
            whisperVoice.StopListening();
        }
    }
    #endregion
}