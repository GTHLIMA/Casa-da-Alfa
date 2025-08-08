using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System;

public class ImageVoiceMatcher : MonoBehaviour, ISpeechToTextListener
{
    #region Data Structures

    /// <summary>
    /// Contém todas as informações de uma única palavra/sílaba da atividade.
    /// </summary>
    [System.Serializable]
    public class SyllableData
    {
        public string word;
        public Sprite image;
        [Tooltip("Dica para o 2º erro.")]
        public AudioClip hintBasicAudio;
        [Tooltip("Dica para o 3º erro.")]
        public AudioClip hintMediumAudio;
        [Tooltip("Dica final (diz a resposta). Toca no 4º erro em diante.")]
        public AudioClip hintFinalAudio;
    }

    /// <summary>
    /// Agrupa uma lista de sílabas, geralmente por vogal, para organização.
    /// </summary>
    [System.Serializable]
    public class VowelDataGroup
    {
        public string groupName;
        public List<SyllableData> syllables;
    }
    #endregion

    #region Public Variables (Inspector Settings)

    [Header("== Configuração Central da Atividade ==")]
    [Tooltip("Qual grupo de vogal usar da lista abaixo? (0 para o primeiro, 1 para o segundo, etc.)")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;
    public string languageCode = "pt-BR";

    [Header("Controles de Similaridade")]
    [Tooltip("Quão similar a palavra falada deve ser da esperada para a maioria das palavras. (0.75 = 75%)")]
    [Range(0f, 1f)]
    public float similarityThreshold = 0.75f;
    [Tooltip("Threshold específico para ZACA. Use um valor menor para ser mais tolerante.")]
    [Range(0f, 1f)]
    public float zacaSimilarityThreshold = 0.70f;
    
    [Header("Referências da Interface (UI)")]
    public Image micIndicatorImage;
    public Animator micIndicatorAnimator;

    [Header("Cores do Indicador de Microfone")]
    public Color promptingColor = Color.red;
    public Color listeningColor = Color.green;
    public Color staticColor = Color.white;

    [Header("Áudios de Feedback")]
    public AudioClip standardPrompt;
    public List<AudioClip> variablePrompts;
    public AudioClip congratulatoryAudio;
    public List<AudioClip> supportAudios;
    
    [Header("Áudios de Feedback Específico")]
    public AudioClip zacaPrompt1;
    public AudioClip zacaPrompt2;

    [Header("Efeitos Visuais")]
    public ParticleSystem endOfLevelConfetti;

    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float delayAfterHint = 1.5f;

    [Header("Animações")]
    public TrainController trainController;

    [Header("========== Pause Menu & Score ==========")]
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
    private bool receivedResult = false;
    private string lastRecognizedText = "";
    private int? lastErrorCode = null;
    private bool isProcessing = false;
    private bool isListening = false;
    private AudioManager audioManager;
    private int score;
    #endregion

    #region Unity Lifecycle Methods

    private void Awake()
    {
        Time.timeScale = 1f;
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
    }

    private void Start()
    {
        Debug.Log("== [ImageVoiceMatcher] - JOGO INICIADO ==");
        
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();
        
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || allVowelData[vowelIndexToPlay].syllables.Count == 0)
        {
            Debug.LogError("ERRO CRÍTICO: 'All Vowel Data' não configurado!");
            return;
        }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        if (micIndicatorImage == null || trainController == null)
        {
            Debug.LogError("ERRO CRÍTICO: 'Mic Indicator Image' ou 'Train Controller' não atribuídos no Inspector!");
            return;
        }

        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor);
        
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        if (isProcessing) return;
        if (isListening)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SpeechToText.Cancel();
                OnResultReceived(currentSyllableList[currentIndex].word, null);
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                SpeechToText.Cancel();
                OnResultReceived("palavra_errada_para_teste", null);
            }
        }
    }
    #endregion

    #region Main Game Flow (Coroutines)

    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(initialDelay);
        CheckForMicrophonePermission();

        if (trainController != null)
        {
            Sprite firstSprite = currentSyllableList[0].image;
            yield return StartCoroutine(trainController.AnimateIn(firstSprite));
        }

        for (currentIndex = 0; currentIndex < currentSyllableList.Count; currentIndex++)
        {
            yield return StartCoroutine(PlayTurnRoutineForCurrentIndex());

            if (currentIndex < currentSyllableList.Count - 1)
            {
                if (trainController != null)
                {
                    Sprite nextSprite = currentSyllableList[currentIndex + 1].image;
                    yield return StartCoroutine(trainController.AdvanceAndChangeImage(currentIndex + 1, nextSprite));
                }
            }
        }
        
        ShowEndPhasePanel();
    }
    
    private IEnumerator PlayTurnRoutineForCurrentIndex()
{
    isProcessing = true;
    mistakeCount = 0;
    bool pularPrompt = false;

    while (true)
    {
        if (!pularPrompt)
        {
            SetMicIndicator(promptingColor);
            AudioClip promptClip = GetCurrentPromptAudio();
            if (audioManager != null && promptClip != null)
            {
                audioManager.PlaySFX(promptClip);
                yield return new WaitForSeconds(promptClip.length + delayAfterHint);
            }
            else { yield return new WaitForSeconds(delayAfterHint); }
        }
        pularPrompt = false;
        
        if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
        receivedResult = false;
        isListening = true;
        SpeechToText.Start(this, true, false);

        // MUDANÇA: A cor verde só é ativada se não for a primeira tentativa.
        if (mistakeCount > 0)
        {
            yield return new WaitForSeconds(1f);
            SetMicIndicator(listeningColor, true);
        }
        
        yield return new WaitUntil(() => receivedResult);
        isListening = false;
        SetMicIndicator(staticColor);
        
        bool isCorrect = false;
        if (lastErrorCode.HasValue && lastErrorCode.Value == 11)
        {
            pularPrompt = true;
            continue;
        }
        if (!lastErrorCode.HasValue && !string.IsNullOrEmpty(lastRecognizedText))
        {
            if (CheckMatch(currentSyllableList[currentIndex].word, lastRecognizedText))
            {
                isCorrect = true;
            }
        }

        if (isCorrect)
        {
            AddScore(10);
            if (audioManager != null && congratulatoryAudio != null)
            {
                audioManager.PlaySFX(congratulatoryAudio);
            }
            yield return new WaitForSeconds(delayAfterCorrect);
            break; 
        }
        
        if (mistakeCount == 0) { pularPrompt = true; }
        mistakeCount++;
    }
    isProcessing = false;
}
    #endregion

    #region Game Logic & Transitions
    private AudioClip GetCurrentPromptAudio()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];

        if (mistakeCount == 0 && currentSyllable.word.ToLower() == "zaca")
        {
            List<AudioClip> zacaPrompts = new List<AudioClip> { zacaPrompt1, zacaPrompt2 };
            zacaPrompts.RemoveAll(item => item == null);
            if (zacaPrompts.Count > 0)
            {
                return zacaPrompts[UnityEngine.Random.Range(0, zacaPrompts.Count)];
            }
        }

        switch (mistakeCount)
        {
            case 0:
                 return (currentIndex < 3 || variablePrompts == null || variablePrompts.Count == 0)
                    ? standardPrompt
                    : variablePrompts[UnityEngine.Random.Range(0, variablePrompts.Count)];
            case 1: 
                return standardPrompt;
            case 2: 
                return currentSyllable.hintBasicAudio;
            case 3: 
                return currentSyllable.hintMediumAudio;
            case 4: 
                return currentSyllable.hintFinalAudio;
            default:
                return (mistakeCount % 2 != 0 && supportAudios != null && supportAudios.Count > 0)
                    ? supportAudios[UnityEngine.Random.Range(0, supportAudios.Count)]
                    : currentSyllable.hintFinalAudio;
        }
    }
    #endregion

    #region STT Interface & Utility Methods
    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        isListening = false;
        lastRecognizedText = recognizedText;
        lastErrorCode = errorCode;
        receivedResult = true;
    }

    public void OnReadyForSpeech() { }
    public void OnBeginningOfSpeech() { }
    public void OnVoiceLevelChanged(float level) { }
    public void OnPartialResultReceived(string partialText) { }

    private void SetMicIndicator(Color color, bool shouldPulse = false)
    {
        if (micIndicatorImage != null) micIndicatorImage.color = color;
        if (micIndicatorAnimator != null) micIndicatorAnimator.SetBool("DevePulsar", shouldPulse);
    }

    private bool CheckMatch(string expected, string received)
    {
        string normalizedExpected = RemoveAccents(expected);
        string normalizedReceived = RemoveAccents(received);

        if ((normalizedExpected == "zaca" && normalizedReceived == "vaca") ||
            (normalizedExpected == "pato" && normalizedReceived == "bato") ||
            (normalizedExpected == "sapo" && normalizedReceived == "zapo"))
        {
            return true;
        }
        
        float thresholdToUse = this.similarityThreshold;
        if (normalizedExpected == "zaca")
        {
            thresholdToUse = this.zacaSimilarityThreshold;
        }
        
        float similarity = 1.0f - ((float)LevenshteinDistance(normalizedExpected, normalizedReceived) / Mathf.Max(normalizedExpected.Length, received.Length));
        return similarity >= thresholdToUse;
    }
    
    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m;
        if (m == 0) return n;
        for (int i = 0; i <= n; d[i, 0] = i++);
        for (int j = 0; j <= m; d[0, j] = j++);
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char c in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void CheckForMicrophonePermission()
    {
        if (!SpeechToText.CheckPermission()) { SpeechToText.RequestPermissionAsync(); }
    }

    private void OnDestroy()
    {
        if (SpeechToText.IsBusy()) { SpeechToText.Cancel(); }
    }
    #endregion

    #region UI & Score Management
    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score.ToString();
        PauseMenu.SetActive(true);
        Time.timeScale = 0;
        ScoreTransfer.Instance?.SetScore(score);
    }

    public void ClosePauseMenu()
    {
        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("== [ShowEndPhasePanel] - FIM DE JOGO! ==");
        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        if (audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3);
        if (endOfLevelConfetti != null) endOfLevelConfetti.Play();
        UpdateAllScoreDisplays();
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        if (numberCounter != null) numberCounter.Value = score;
        ScoreTransfer.Instance?.SetScore(score);
        UpdateAllScoreDisplays();
    }

    private void UpdateAllScoreDisplays()
    {
        string formattedScore = score.ToString("000");
        if (scoreHUD != null) scoreHUD.text = formattedScore;
        if (scorePause != null) scorePause.text = "Score: " + formattedScore;
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + formattedScore;
    }
    #endregion
}