using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

public class ImageVoiceMatcher : MonoBehaviour, ISpeechToTextListener
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
    [Header("== Configuração Central da Atividade ==")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;
    public string languageCode = "pt-BR";

    [Header("Controles de Similaridade")]
    [Range(0f, 1f)] public float similarityThreshold = 0.75f;
    [Range(0f, 1f)] public float zacaSimilarityThreshold = 0.70f;

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
    public float delayAfterPromptBeforeReveal = 0.5f;

    [Header("Animações")]
    public TrainController trainController;

    [Header("========== Pause Menu & Score ==========")]
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;

    [Header("Controle de Microfone")]
    [Tooltip("Ativa ou desativa o microfone durante o jogo")]
    public bool activateMicrophone = true;
    [Tooltip("Delay em segundos antes do microfone ser ativado")]
    public float micActivationDelay = 0.5f;
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

    #region Unity Lifecycle
    private void Awake()
    {
        Time.timeScale = 1f;
        audioManager = FindObjectOfType<AudioManager>();
    }

    private void Start()
    {
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
            if (Input.GetKeyDown(KeyCode.C)) { OnResultReceived(currentSyllableList[currentIndex].word, null); }
            if (Input.GetKeyDown(KeyCode.X)) { OnResultReceived("palavra_errada", null); }
        }
    }
    #endregion

    #region Main Game Flow
    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(initialDelay);
        CheckForMicrophonePermission();

        if (trainController != null)
        {
            AudioClip firstPrompt = GetCurrentPromptAudio();
            yield return StartCoroutine(trainController.AnimateIn(firstPrompt));
        }

        for (currentIndex = 0; currentIndex < currentSyllableList.Count; currentIndex++)
        {
            yield return StartCoroutine(PlayTurnRoutineForCurrentIndex());

            if (trainController != null)
            {
                trainController.MarkWagonAsCompleted(currentIndex);
            }

            mistakeCount = 0;

            if (currentIndex < currentSyllableList.Count - 1 && trainController != null)
            {
                AudioClip nextPrompt = GetCurrentPromptAudio(currentIndex + 1);
                yield return StartCoroutine(trainController.AdvanceToNextWagon(currentIndex + 1, nextPrompt));
            }
        }

        ShowEndPhasePanel();
    }

    private IEnumerator PlayTurnRoutineForCurrentIndex()
    {
        isProcessing = true;
        mistakeCount = 0;
        bool isAndroid = Application.platform == RuntimePlatform.Android && !Application.isEditor;

        if (!SpeechToText.CheckPermission())
        {
            Debug.LogWarning("Permissão de microfone não concedida");
            isProcessing = false;
            yield break;
        }

        // Primeira ativação do microfone com delay
        if (activateMicrophone && isAndroid && STTIsInitialized())
        {
            yield return new WaitForSeconds(micActivationDelay);
            Debug.Log("Microfone ativado: aguardando fala da criança");
            isListening = true;
            SpeechToText.Start(this, true, false);
            SetMicIndicator(listeningColor, true);
        }
        else
        {
            Debug.Log("STT não disponível ou microfone desativado - simulando input");
        }

        yield return new WaitForSeconds(delayAfterPromptBeforeReveal);

        if (trainController != null)
        {
            Sprite currentSprite = currentSyllableList[currentIndex].image;
            yield return StartCoroutine(trainController.RevealCurrentImage(currentSprite));
        }

        bool pularPromptNaProximaTentativa = false;

        while (true)
        {
            if (pularPromptNaProximaTentativa)
            {
                AudioClip hintClip = GetCurrentPromptAudio();
                if (audioManager != null && hintClip != null)
                {
                    audioManager.PlaySFX(hintClip);
                    yield return new WaitForSeconds(hintClip.length + delayAfterHint);
                }
            }
            pularPromptNaProximaTentativa = false;

            // Reativação do microfone com delay
            if (activateMicrophone && isAndroid && !isListening && STTIsInitialized())
            {
                yield return new WaitForSeconds(micActivationDelay);
                Debug.Log("Reativando microfone para próxima tentativa");
                isListening = true;
                SpeechToText.Start(this, true, false);
                SetMicIndicator(listeningColor, true);
            }

            float waitTime = 0f;
            receivedResult = false;

            while (!receivedResult && waitTime < 10f)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            if (!receivedResult)
            {
                Debug.Log("Timeout: 10 segundos sem fala detectada");
                isListening = false;
                SetMicIndicator(staticColor);
                mistakeCount++;
                pularPromptNaProximaTentativa = true;

                // Reativação do microfone após timeout com delay
                if (activateMicrophone && isAndroid && STTIsInitialized())
                {
                    yield return new WaitForSeconds(micActivationDelay);
                    Debug.Log("Reativando microfone após timeout");
                    isListening = true;
                    SpeechToText.Start(this, true, false);
                    SetMicIndicator(listeningColor, true);
                }

                continue;
            }

            isListening = false;
            SetMicIndicator(staticColor);

            if (lastErrorCode.HasValue && (lastErrorCode.Value == 6 || lastErrorCode.Value == 7)) continue;

            bool isCorrect = !lastErrorCode.HasValue && !string.IsNullOrEmpty(lastRecognizedText) &&
                             CheckMatch(currentSyllableList[currentIndex].word, lastRecognizedText);

            if (isCorrect)
            {
                AddScore(10);
                if (audioManager != null && congratulatoryAudio != null)
                    audioManager.PlaySFX(congratulatoryAudio);

                yield return new WaitForSeconds(delayAfterCorrect);
                break;
            }

            mistakeCount++;
            pularPromptNaProximaTentativa = true;
        }

        isProcessing = false;
    }
    #endregion

    #region Prompts
    AudioClip GetCurrentPromptAudio(int specificIndex = -1)
    {
        int indexToUse = (specificIndex == -1) ? currentIndex : specificIndex;
        if (indexToUse < 0 || indexToUse >= currentSyllableList.Count) return null;

        SyllableData currentSyllable = currentSyllableList[indexToUse];

        if (mistakeCount == 0)
        {
            if (currentSyllable.word.ToLower() == "zaca" && zacaPrompt1 != null) return zacaPrompt1;
            if (variablePrompts != null && variablePrompts.Count > 0)
                return variablePrompts[UnityEngine.Random.Range(0, variablePrompts.Count)];
            return standardPrompt;
        }

        switch (mistakeCount)
        {
            case 1: return currentSyllable.hintBasicAudio;
            default: return currentSyllable.hintFinalAudio;
        }
    }
    #endregion

    #region STT Interface
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
            (normalizedExpected == "sapo" && normalizedReceived == "zapo")) return true;

        float thresholdToUse = normalizedExpected == "zaca" ? zacaSimilarityThreshold : similarityThreshold;

        float similarity = 1f - ((float)LevenshteinDistance(normalizedExpected, normalizedReceived) /
                                 Mathf.Max(normalizedExpected.Length, received.Length));

        return similarity >= thresholdToUse;
    }

    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m;
        if (m == 0) return n;
        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;
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
        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private void CheckForMicrophonePermission()
    {
        if (!SpeechToText.CheckPermission()) SpeechToText.RequestPermissionAsync();
    }

    private void OnDestroy()
    {
        if (SpeechToText.IsBusy()) SpeechToText.Cancel();
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
        if (scoreEndPhase != null) scoreEndPhase.text = formattedScore;
    }
    #endregion

    private bool STTIsInitialized()
    {
        return true; 
    }
}
