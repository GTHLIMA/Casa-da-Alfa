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
    [Tooltip("Quão similar a palavra falada deve ser da esperada? (0.75 = 75%)")]
    [Range(0f, 1f)]
    public float similarityThreshold = 0.75f;

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

    [Header("Efeitos Visuais")]
    public ParticleSystem endOfLevelConfetti;

    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float delayAfterHint = 1.5f;

    [Header("========== Pause Menu & Score ==========")]
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;

    [Header("Animações")]
    [Tooltip("Referência ao script que controla a animação do trem.")]
    public TrainAnimator trainAnimator;
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
            Debug.LogError("ERRO CRÍTICO: 'All Vowel Data' não configurado ou 'vowelIndexToPlay' é inválido! O jogo não pode continuar.");
            return;
        }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        if (micIndicatorImage == null)
        {
            Debug.LogError("ERRO CRÍTICO: Referência de UI 'Mic Indicator Image' não atribuída! O jogo não pode continuar.");
            return;
        }

        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor);
        StartCoroutine(GameStartSequence());
    }

    private void Update()
    {
        if (isProcessing) return;

        if (isListening)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.LogWarning("--- DEBUG: Tecla C Pressionada -> Forçando Acerto ---");
                SpeechToText.Cancel();
                OnResultReceived(currentSyllableList[currentIndex].word, null);
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.LogWarning("--- DEBUG: Tecla X Pressionada -> Forçando Erro ---");
                SpeechToText.Cancel();
                OnResultReceived("palavra_errada_para_teste", null);
            }
        }
    }
    #endregion

    #region Main Game Flow (Coroutines)

    private IEnumerator GameStartSequence()
    {
        Debug.Log($"[GameStartSequence] - Iniciando delay de {initialDelay} segundos.");
        yield return new WaitForSeconds(initialDelay);
        Debug.Log("[GameStartSequence] - Verificando permissão do microfone.");
        CheckForMicrophonePermission();
        Debug.Log("[GameStartSequence] - Sequência inicial completa. Começando o primeiro turno.");
        StartCoroutine(PlayTurnRoutine());
    }

    /// <summary>
/// O CORAÇÃO DO JOGO: gerencia o ciclo completo de um turno, incluindo a animação do trem.
/// </summary>
private IEnumerator PlayTurnRoutine()
{
    Debug.Log($"== [PlayTurnRoutine] - INICIANDO NOVO TURNO para: '{currentSyllableList[currentIndex].word}' ==");
    isProcessing = true;
    
    // --- ETAPA 1: O TREM ENTRA COM A IMAGEM CORRETA ---
    SetMicIndicator(promptingColor, false);
    if (trainAnimator != null)
    {
        Sprite spriteDaVez = currentSyllableList[currentIndex].image;
        yield return StartCoroutine(trainAnimator.AnimateIn(spriteDaVez));
    }

    // --- ETAPA 2: LOOP DE TENTATIVA E ERRO (O TREM FICA PARADO) ---
    bool pularPrompt = false;
    while (true)
    {
        // FASE 1: PERGUNTA/DICA
        if (!pularPrompt)
        {
            AudioClip promptClip = GetCurrentPromptAudio();
            if (audioManager != null && promptClip != null)
            {
                audioManager.PlaySFX(promptClip);
                yield return new WaitForSeconds(promptClip.length + delayAfterHint);
            }
            else { yield return new WaitForSeconds(delayAfterHint); }
        }
        pularPrompt = false;

        // FASE 2: ESCUTA
        if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
        receivedResult = false;
        isListening = true;
        SpeechToText.Start(this, true, false);
        yield return new WaitForSeconds(1f);
        SetMicIndicator(listeningColor, true);
        yield return new WaitUntil(() => receivedResult);
        isListening = false;
        SetMicIndicator(staticColor);

        // --- FASE 3: PROCESSAMENTO DO RESULTADO (LÓGICA CORRIGIDA) ---
        bool isCorrect = false;

        // Ignora o erro 11 e prepara para pular o prompt na próxima tentativa silenciosa.
        if (lastErrorCode.HasValue && lastErrorCode.Value == 11)
        {
            pularPrompt = true;
            continue; // Pula para a próxima iteração do loop, reiniciando a escuta
        }
        
        // Verifica se o resultado é válido e se a palavra bate
        if (!lastErrorCode.HasValue && !string.IsNullOrEmpty(lastRecognizedText))
        {
            string expectedWord = currentSyllableList[currentIndex].word.ToLower().Trim();
            string receivedWord = lastRecognizedText.ToLower().Trim();
            if (CheckMatch(expectedWord, receivedWord))
            {
                isCorrect = true;
            }
        }

        // Se a resposta foi correta, sai do loop de tentativas.
        if (isCorrect)
        {
            Debug.Log("[PlayTurnRoutine] - ACERTOU! Saindo do loop de tentativas.");
            break; 
        }
        
        // Se chegou até aqui, é porque foi um erro (palavra errada, vazia ou erro do plugin).
        // FASE 4: TRATAMENTO DO ERRO
        if (mistakeCount == 0)
        {
            pularPrompt = true; // Prepara para a tentativa silenciosa
        }
        mistakeCount++;
        Debug.LogWarning($"[PlayTurnRoutine] - Erro detectado. Contagem de erros: {mistakeCount}");
    }

    // --- ETAPA 4: RESPOSTA CORRETA - SOM DE PARABÉNS E ANIMAÇÃO DE SAÍDA ---
    AddScore(10);
    if (audioManager != null && congratulatoryAudio != null)
    {
        audioManager.PlaySFX(congratulatoryAudio);
        yield return new WaitForSeconds(congratulatoryAudio.length);
    }
    else { yield return new WaitForSeconds(delayAfterCorrect); }

    SetMicIndicator(promptingColor, false);
    if (trainAnimator != null)
    {
        yield return StartCoroutine(trainAnimator.AnimateOut());
    }

    // --- ETAPA 5: PREPARAÇÃO PARA O PRÓXIMO TURNO ---
    isProcessing = false;
    GoToNextImage();
}

/// <summary>
/// Apenas prepara o índice para o próximo turno.
/// </summary>
private void GoToNextImage()
{
    mistakeCount = 0;
    currentIndex++;
    Debug.Log("== [GoToNextImage] - Preparando índice: " + currentIndex + " ==");
    if (currentIndex >= currentSyllableList.Count)
    {
        ShowEndPhasePanel();
    }
    else
    {
        StartCoroutine(PlayTurnRoutine());
    }
}
    #endregion

    #region Game Logic & Transitions

    private AudioClip GetCurrentPromptAudio()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];
        AudioClip chosenClip;

        switch (mistakeCount)
        {
            case 0:
                chosenClip = (currentIndex < 3 || variablePrompts == null || variablePrompts.Count == 0)
                    ? standardPrompt
                    : variablePrompts[UnityEngine.Random.Range(0, variablePrompts.Count)];
                break;
            case 1:
                chosenClip = standardPrompt;
                break;
            case 2:
                chosenClip = currentSyllable.hintBasicAudio;
                break;
            case 3:
                chosenClip = currentSyllable.hintMediumAudio;
                break;
            case 4:
                chosenClip = currentSyllable.hintFinalAudio;
                break;
            default:
                if (mistakeCount % 2 != 0 && supportAudios != null && supportAudios.Count > 0)
                {
                    chosenClip = supportAudios[UnityEngine.Random.Range(0, supportAudios.Count)];
                }
                else
                {
                    chosenClip = currentSyllable.hintFinalAudio;
                }
                break;
        }
        Debug.Log($"[GetCurrentPromptAudio] - Escolhido áudio: {(chosenClip != null ? chosenClip.name : "NENHUM")}");
        return chosenClip;
    }

    private IEnumerator HandleCorrectAnswerFlow()
    {
        Debug.Log("== [HandleCorrectAnswerFlow] - Fluxo de ACERTO iniciado. ==");
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
            yield return new WaitForSeconds(congratulatoryAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(delayAfterCorrect);
        }

        SetMicIndicator(promptingColor, false);
        if (trainAnimator != null)
        {
            yield return StartCoroutine(trainAnimator.AnimateOut());
        }
    }
    #endregion

    #region STT Interface & Utility Methods

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        Debug.Log($"<<<<< [OnResultReceived] - PLUGIN RETORNOU! Texto: '{recognizedText}', Código de Erro: {(errorCode.HasValue ? errorCode.Value.ToString() : "Nenhum")} >>>>>");
        isListening = false;
        lastRecognizedText = recognizedText;
        lastErrorCode = errorCode;
        receivedResult = true;
    }

    public void OnReadyForSpeech() { Debug.Log("[STT] - Status: Pronto para ouvir."); }
    public void OnBeginningOfSpeech() { Debug.Log("[STT] - Status: Usuário começou a falar."); }
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
        Debug.Log($"--- COMPARANDO (SEM ACENTOS)! Esperado: '{normalizedExpected}' | Recebido: '{normalizedReceived}' ---");
        float similarity = 1.0f - ((float)LevenshteinDistance(normalizedExpected, normalizedReceived) / Mathf.Max(normalizedExpected.Length, received.Length));
        Debug.Log($"--- Similaridade: {similarity:P2} ---");
        return similarity >= similarityThreshold || normalizedReceived.Contains(normalizedExpected);
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
        if (!SpeechToText.CheckPermission())
        {
            SpeechToText.RequestPermissionAsync();
        }
    }

    private void OnDestroy()
    {
        if (SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
        }
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