using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ImageVoiceMatcher : MonoBehaviour, ISpeechToTextListener
{
    // --- ESTRUTURA DE DADOS ---
    [System.Serializable]
    public class SyllableData
    {
        public string word;
        public Sprite image;
        public AudioClip hintAudio;
    }

    [System.Serializable]
    public class VowelDataGroup
    {
        public string groupName;
        public List<SyllableData> syllables;
    }

    [Header("== Configuração Central da Atividade ==")]
    [Tooltip("Qual grupo de vogal usar da lista abaixo? (0 para o primeiro, 1 para o segundo, etc.)")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;
    public string languageCode = "pt-BR";

    [Header("Referências da Interface (UI)")]
    public Image displayImage;
    public Button listenButton;
    public Animator listenButtonAnimator;

    [Header("Áudios de Feedback e Dicas")]
    public AudioClip explanationAudio;
    public AudioClip congratulatoryAudio;
    public List<AudioClip> tryAgainAudios;
    public AudioClip inactivityPromptAudio;

    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float inactivityTimeout = 10f;
    public float fadeDuration = 0.5f;

    // --- Variáveis Internas ---
    private List<SyllableData> currentSyllableList;
    private int currentIndex = 0;
    private int mistakeCount = 0;
    private float inactivityTimer = 0f;
    private bool isListening = false;
    private bool isProcessing = false;
    private bool gameReady = false;
    private AudioManager audioManager;

    [Header("========== Pause Menu & Score ==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    public ParticleSystem confettiEffect;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;

    void Awake()
    {
        Time.timeScale = 1f;
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
    }

    void Start()
    {
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();

        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay) { Debug.LogError("ERRO: 'allVowelData' não configurado ou 'vowelIndexToPlay' é inválido!"); return; }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;
        if (currentSyllableList.Count == 0) { Debug.LogError("ERRO: A lista de sílabas para a vogal selecionada está vazia!"); return; }
        if (displayImage == null) { Debug.LogError("ERRO: 'displayImage' não foi atribuído no Inspector!"); return; }

        SpeechToText.Initialize(languageCode);
        if (listenButton != null)
        {
            listenButton.onClick.AddListener(OnListenButtonPressed);
            listenButton.interactable = false;
        }
        if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);

        ShowImage(currentIndex);
        StartCoroutine(GameStartSequence());
        CheckForMicrophonePermission();
    }

    void Update()
    {
        if (!gameReady || isListening || isProcessing)
        {
            if (isListening || isProcessing) inactivityTimer = 0f;
            return;
        }

        if (listenButton != null && listenButton.interactable)
        {
            inactivityTimer += Time.deltaTime;
            if (inactivityTimer >= inactivityTimeout)
            {
                if (listenButtonAnimator != null && !listenButtonAnimator.GetBool("DevePulsar"))
                {
                    listenButtonAnimator.SetBool("DevePulsar", true);
                    if (audioManager != null && inactivityPromptAudio != null) audioManager.PlaySFX(inactivityPromptAudio);
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.C)) { StartCoroutine(HandleCorrectAnswerFlow()); }
        if (Input.GetKeyDown(KeyCode.X)) { StartCoroutine(HandleWrongAnswerOrErrorFlow()); }
    }

    private IEnumerator GameStartSequence()
    {
        yield return new WaitForSeconds(initialDelay);
        if (audioManager != null && explanationAudio != null) audioManager.PlaySFX(explanationAudio);
        gameReady = true;
        TryEnableListenButton();
    }

    void TryEnableListenButton()
    {
        if (gameReady && listenButton != null && SpeechToText.CheckPermission())
        {
            listenButton.interactable = true;
            if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);
            inactivityTimer = 0f;
        }
    }

    void OnListenButtonPressed()
    {
        inactivityTimer = 0f;
        if (!SpeechToText.CheckPermission()) { CheckForMicrophonePermission(); return; }
        if (isListening || isProcessing) return;
        if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);
        isListening = true;
        if (listenButton != null) listenButton.interactable = false;
        SpeechToText.Start(this, true, false);
    }

    // Apenas UMA versão deste método, que só troca o sprite
    void ShowImage(int index)
    {
        if (index < 0 || index >= currentSyllableList.Count) return;
        if (displayImage == null) return;
        displayImage.sprite = currentSyllableList[index].image;
        displayImage.preserveAspect = true;
    }

    void GoToNextImage()
    {
        mistakeCount = 0;
        currentIndex++;
        if (currentIndex >= currentSyllableList.Count)
        {
            Debug.Log("FIM DA LISTA DE SÍLABAS!");
            if (listenButton != null) listenButton.interactable = false;
            if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);
            ShowEndPhasePanel();
        }
        else
        {
            StartCoroutine(FadeTransitionToShowNextImage());
        }
    }

    private IEnumerator FadeTransitionToShowNextImage()
    {
        if (listenButton != null) listenButton.interactable = false;
        isProcessing = true;
        float elapsedTime = 0f;
        Color originalColor = Color.white;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            displayImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }
        displayImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        ShowImage(currentIndex);
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            displayImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }
        displayImage.color = Color.white; // CORRIGIDO: Garante que a cor final é totalmente opaca
        isProcessing = false;
        TryEnableListenButton();
    }

    private IEnumerator HandleCorrectAnswerFlow()
    {
        isProcessing = true;
        if (listenButton != null) listenButton.interactable = false;
        if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
            yield return new WaitForSeconds(congratulatoryAudio.length);
        }
        yield return new WaitForSeconds(delayAfterCorrect);
        GoToNextImage();
        // 'isProcessing' é resetado ao fim da corrotina de fade
    }

    private IEnumerator HandleWrongAnswerOrErrorFlow()
    {
        isProcessing = true;
        isListening = false;
        if (listenButton != null) listenButton.interactable = false;
        if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", false);
        mistakeCount++;
        if (mistakeCount == 1)
        {
            AudioClip hintClip = currentSyllableList[currentIndex].hintAudio;
            if (audioManager != null && hintClip != null)
            {
                audioManager.PlaySFX(hintClip);
                yield return new WaitForSeconds(hintClip.length);
            }
        }
        else
        {
            if (audioManager != null && tryAgainAudios != null && tryAgainAudios.Count > 0)
            {
                AudioClip clipToPlay = tryAgainAudios[Random.Range(0, tryAgainAudios.Count)];
                audioManager.PlaySFX(clipToPlay);
                yield return new WaitForSeconds(clipToPlay.length);
            }
        }
        yield return new WaitForSeconds(0.5f);
        isProcessing = false;
        TryEnableListenButton();
    }
    
    // --- MÉTODOS DA INTERFACE ISpeechToTextListener ---
    public void OnReadyForSpeech() { }
    public void OnBeginningOfSpeech() { }
    public void OnVoiceLevelChanged(float level) { }
    public void OnPartialResultReceived(string partialText) { }

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        isListening = false;
        if (isProcessing) return;
        if (errorCode.HasValue || string.IsNullOrEmpty(recognizedText)) { StartCoroutine(HandleWrongAnswerOrErrorFlow()); return; }
        isProcessing = true;
        string expectedWord = currentSyllableList[currentIndex].word.ToLower().Trim();
        string receivedWord = recognizedText.ToLower().Trim();
        bool matched = false;
        if (expectedWord == "zaca") { if (receivedWord.Contains("zaca") || receivedWord.Contains("saca")) matched = true; }
        else { if (receivedWord.Contains(expectedWord)) matched = true; }
        if (matched) { StartCoroutine(HandleCorrectAnswerFlow()); }
        else { StartCoroutine(HandleWrongAnswerOrErrorFlow()); }
    }

    void CheckForMicrophonePermission()
    {
        if (!SpeechToText.CheckPermission())
        {
            Debug.LogError("Permissão de microfone NÃO CONCEDIDA.");
            if (listenButton != null) listenButton.interactable = false;
        }
    }
    
    void OnDestroy()
    {
        if (listenButton != null) listenButton.onClick.RemoveListener(OnListenButtonPressed);
        if (SpeechToText.IsBusy()) SpeechToText.Cancel();
    }

    #region Pause Menu and Score Management
    public void ClosePauseMenu()
    {
        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score.ToString();
        PauseMenu.SetActive(true);
        Time.timeScale = 0;
        ScoreTransfer.Instance.SetScore(score);
    }

    public void ShowEndPhasePanel()
    {
    Debug.Log("ImageVoiceMatcher ShowEndPhasePanel: CHAMADO. Score: " + score);
    if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString();
    if (endPhasePanel != null) endPhasePanel.SetActive(true);

    // Toca o som de fim de fase
    if (audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3);

    if (confettiEffect != null)
    {
        confettiEffect.Play(); // Dispara o sistema de partículas para tocar uma vez
        Debug.Log("Efeito de confete ativado!");
    }
    else
    {
        Debug.LogWarning("O efeito de confete (Confetti Effect) não foi atribuído no Inspector!");
    }

    ScoreTransfer.Instance.SetScore(score);
    // Time.timeScale = 0f; // Descomente esta linha se quiser que o jogo pause na tela final
    }
    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        if (numberCounter != null) numberCounter.Value = score;
        ScoreTransfer.Instance.SetScore(score);
        UpdateAllScoreDisplays();
    }

    void UpdateAllScoreDisplays()
    {
        string formattedScore = score.ToString("000");
        if (scoreHUD != null) scoreHUD.text = formattedScore;
        if (scorePause != null) scorePause.text = "Score: " + formattedScore;
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + formattedScore;
    }
    #endregion
}