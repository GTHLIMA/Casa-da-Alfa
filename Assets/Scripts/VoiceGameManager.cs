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
    // --- ESTRUTURA DE DADOS ---
    [System.Serializable]
    public class SyllableData { public string word; public Sprite image; public AudioClip hintBasicAudio; public AudioClip hintMediumAudio; public AudioClip hintFinalAudio; }
    [System.Serializable]
    public class VowelDataGroup { public string groupName; public List<SyllableData> syllables; }

    [Header("== Configuração Central da Atividade ==")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;
    public string languageCode = "pt-BR";
    [Tooltip("Quão similar a palavra falada deve ser da esperada? (0.75 = 75%)")]
    [Range(0f, 1f)]
    public float similarityThreshold = 0.75f;

    [Header("Referências da Interface (UI)")]
    public Image displayImage;
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

    [Header("Efeitos Visuais")]
    public ParticleSystem endOfLevelConfetti;

    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float delayAfterHint = 1.5f;
    public float fadeDuration = 0.5f;
    
    private List<SyllableData> currentSyllableList;
    private int currentIndex = 0;
    private int mistakeCount = 0;
    private bool receivedResult = false;
    private string lastRecognizedText = "";
    private int? lastErrorCode = null;
    private bool isProcessing = false;
    private bool isListening = false;
    private bool gameReady = false;
    private AudioManager audioManager;
    
    [Header("========== Pause Menu & Score ==========")]
    private int score;
    public TMP_Text scorePause, scoreEndPhase, scoreHUD;
    public GameObject PauseMenu;
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
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || allVowelData[vowelIndexToPlay].syllables.Count == 0) { Debug.LogError("ERRO: Configuração da lista de palavras ('All Vowel Data') inválida!"); return; }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;
        if (displayImage == null || micIndicatorImage == null) { Debug.LogError("ERRO: Referências de UI não atribuídas!"); return; }
        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor);
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        yield return new WaitForSeconds(initialDelay);
        CheckForMicrophonePermission();
        StartCoroutine(PlayTurnRoutine());
    }

    private IEnumerator PlayTurnRoutine()
    {
        Debug.Log("== [PlayTurnRoutine] - INICIANDO NOVO TURNO para imagem #" + currentIndex + " ==");
        isProcessing = true;
        yield return StartCoroutine(FadeImage(true));
        while (true)
        {
            Debug.Log("[PlayTurnRoutine] - FASE DA PERGUNTA/DICA (Microfone Vermelho)");
            SetMicIndicator(promptingColor);
            AudioClip promptClip = GetCurrentPromptAudio();
            if (audioManager != null && promptClip != null)
            {
                audioManager.PlaySFX(promptClip);
                yield return new WaitForSeconds(promptClip.length);
            } else { yield return new WaitForSeconds(delayAfterHint); }

            if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
            
            Debug.Log("[PlayTurnRoutine] - FASE DE ESCUTA (Microfone Verde)");
            SetMicIndicator(listeningColor, true);
            receivedResult = false;
            isListening = true;
            SpeechToText.Start(this, true, false);

            Debug.Log("[PlayTurnRoutine] - ...Aguardando resultado da voz...");
            yield return new WaitUntil(() => receivedResult);
            isListening = false;
            SetMicIndicator(staticColor);
            Debug.Log("[PlayTurnRoutine] - RESULTADO DA VOZ RECEBIDO! PROCESSANDO...");

            if (lastErrorCode.HasValue)
            {
                Debug.LogError("[PlayTurnRoutine] - Plugin de voz retornou um CÓDIGO DE ERRO: " + lastErrorCode.Value);
                mistakeCount++;
                continue;
            }
            if (string.IsNullOrEmpty(lastRecognizedText))
            {
                Debug.LogWarning("[PlayTurnRoutine] - Plugin de voz não reconheceu texto (resultado vazio).");
                mistakeCount++;
                continue;
            }

            string expectedWord = currentSyllableList[currentIndex].word.ToLower().Trim();
            string receivedWord = lastRecognizedText.ToLower().Trim();
            bool matched = CheckMatch(expectedWord, receivedWord);

            Debug.Log($"[PlayTurnRoutine] - Verificação do Match retornou: {matched}");

            if (matched)
            {
                yield return StartCoroutine(HandleCorrectAnswerFlow());
                break;
            }
            else
            {
                mistakeCount++;
                Debug.Log("[PlayTurnRoutine] - Erro. mistakeCount agora é: " + mistakeCount + ". Reiniciando loop de tentativa.");
            }
        }
        
        Debug.Log("[PlayTurnRoutine] - Fim do turno. Preparando para a próxima imagem.");
        yield return StartCoroutine(FadeImage(false));
        isProcessing = false;
        GoToNextImage();
    }
    
    private AudioClip GetCurrentPromptAudio() { /* ...código sem alterações... */ SyllableData currentSyllable = currentSyllableList[currentIndex]; switch (mistakeCount) { case 0: if (currentIndex < 3) { return standardPrompt; } else { if (variablePrompts != null && variablePrompts.Count > 0) return variablePrompts[UnityEngine.Random.Range(0, variablePrompts.Count)]; else return standardPrompt; } case 1: return standardPrompt; case 2: return currentSyllable.hintBasicAudio; case 3: return currentSyllable.hintMediumAudio; case 4: return currentSyllable.hintFinalAudio; default: if (mistakeCount % 2 != 0) { if (supportAudios != null && supportAudios.Count > 0) return supportAudios[UnityEngine.Random.Range(0, supportAudios.Count)]; else return currentSyllable.hintFinalAudio; } else { return currentSyllable.hintFinalAudio; } } }
    void GoToNextImage() { mistakeCount = 0; currentIndex++; if (currentIndex >= currentSyllableList.Count) { ShowEndPhasePanel(); } else { StartCoroutine(PlayTurnRoutine()); } }
    
    private IEnumerator HandleCorrectAnswerFlow()
    {
        Debug.Log("== [HandleCorrectAnswerFlow] - Fluxo de ACERTO iniciado. ==");
        SetMicIndicator(staticColor);
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null) { audioManager.PlaySFX(congratulatoryAudio); yield return new WaitForSeconds(congratulatoryAudio.length); }
        yield return new WaitForSeconds(delayAfterCorrect);
    }
    
    void SetMicIndicator(Color color, bool shouldPulse = false) { if (micIndicatorImage != null) micIndicatorImage.color = color; if (micIndicatorAnimator != null) micIndicatorAnimator.SetBool("DevePulsar", shouldPulse); }

    // --- A FUNÇÃO MAIS IMPORTANTE PARA O DEBUG ---
    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        // Este log é o PRIMEIRO a ser chamado quando o plugin retorna um resultado.
        Debug.Log($"<<<<< [OnResultReceived] - PLUGIN RETORNOU! Texto: '{recognizedText}', Código de Erro: {(errorCode.HasValue ? errorCode.Value.ToString() : "Nenhum")} >>>>>");
        
        isListening = false;
        lastRecognizedText = recognizedText;
        lastErrorCode = errorCode;
        receivedResult = true;
    }

    #region Outros Métodos (com logs adicionados)
    public void OnReadyForSpeech() { Debug.Log("[STT] - Status: Pronto para ouvir."); }
    public void OnBeginningOfSpeech() { Debug.Log("[STT] - Status: Usuário começou a falar."); }
    public void OnVoiceLevelChanged(float level) {}
    public void OnPartialResultReceived(string partialText) {}
    private bool CheckMatch(string expected, string received) { string normalizedExpected = RemoveAccents(expected); string normalizedReceived = RemoveAccents(received); Debug.Log($"--- COMPARANDO (SEM ACENTOS)! Esperado: '{normalizedExpected}' | Recebido: '{normalizedReceived}' ---"); float similarity = 1.0f - ((float)LevenshteinDistance(normalizedExpected, normalizedReceived) / Mathf.Max(normalizedExpected.Length, received.Length)); Debug.Log($"--- Similaridade: {similarity:P2} ---"); return similarity >= similarityThreshold || normalizedReceived.Contains(normalizedExpected); }
    public int LevenshteinDistance(string s, string t) { int n = s.Length; int m = t.Length; int[,] d = new int[n + 1, m + 1]; if (n == 0) return m; if (m == 0) return n; for (int i = 0; i <= n; d[i, 0] = i++); for (int j = 0; j <= m; d[0, j] = j++); for (int i = 1; i <= n; i++) { for (int j = 1; j <= m; j++) { int cost = (t[j - 1] == s[i - 1]) ? 0 : 1; d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost); } } return d[n, m]; }
    private string RemoveAccents(string text) { if (string.IsNullOrEmpty(text)) return text; text = text.Normalize(NormalizationForm.FormD); StringBuilder stringBuilder = new StringBuilder(); foreach (var c in text) { var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c); if (unicodeCategory != UnicodeCategory.NonSpacingMark) { stringBuilder.Append(c); } } return stringBuilder.ToString().Normalize(NormalizationForm.FormC); }
    void CheckForMicrophonePermission() { if (!SpeechToText.CheckPermission()) SpeechToText.RequestPermissionAsync(); }
    void OnDestroy() { if (SpeechToText.IsBusy()) SpeechToText.Cancel(); }
    private IEnumerator FadeImage(bool fadeIn) { float targetAlpha = fadeIn ? 1f : 0f; float startAlpha = displayImage.color.a; if (fadeIn) ShowImage(currentIndex); float elapsedTime = 0f; while (elapsedTime < fadeDuration) { elapsedTime += Time.deltaTime; float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration); displayImage.color = new Color(1, 1, 1, newAlpha); yield return null; } displayImage.color = new Color(1, 1, 1, targetAlpha); }
    void ShowImage(int index) { if (index < 0 || index >= currentSyllableList.Count) return; displayImage.sprite = currentSyllableList[index].image; }
    #endregion
    
    #region Pause Menu and Score Management
    public void ClosePauseMenu() { PauseMenu.SetActive(false); Time.timeScale = 1f; }
    public void OpenPauseMenu() { if (scorePause != null) scorePause.text = "Score: " + score.ToString(); PauseMenu.SetActive(true); Time.timeScale = 0; ScoreTransfer.Instance?.SetScore(score); }
    public void ShowEndPhasePanel() { if (endPhasePanel != null) endPhasePanel.SetActive(true); if (audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3); if (endOfLevelConfetti != null) endOfLevelConfetti.Play(); UpdateAllScoreDisplays(); }
    public void AddScore(int amount) { score += amount; if (score < 0) score = 0; if (numberCounter != null) numberCounter.Value = score; ScoreTransfer.Instance?.SetScore(score); UpdateAllScoreDisplays(); }
    void UpdateAllScoreDisplays() { string formattedScore = score.ToString("000"); if (scoreHUD != null) scoreHUD.text = formattedScore; if (scorePause != null) scorePause.text = "Score: " + formattedScore; if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + formattedScore; }
    #endregion
}