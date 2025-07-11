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

    [Tooltip("Threshold de similaridade específico para a palavra ZACA. Use um valor menor para ser mais tolerante.")]
    [Range(0f, 1f)]
    public float zacaSimilarityThreshold = 0.70f;


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

    [Header("Animações")]
    [Tooltip("Referência ao script que controla a animação do trem.")]
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

    [Header("Áudios de Feedback Específico")]
    [Tooltip("Pergunta específica para a palavra ZACA (Ex: Quem é esse?)")]
    public AudioClip zacaPrompt1;
    [Tooltip("Segunda opção de pergunta para ZACA (Ex: Você conhece essa pessoa?)")]
    public AudioClip zacaPrompt2;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Awake é chamado quando a instância do script é carregada. Ideal para inicializações que não dependem de outros scripts.
    /// </summary>
    private void Awake()
    {
        Time.timeScale = 1f;
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
    }

    /// <summary>
    /// Start é chamado no primeiro frame. Usado para configurar o estado inicial do jogo e validar referências.
    /// </summary>
    private void Start()
    {
        Debug.Log("== [ImageVoiceMatcher] - JOGO INICIADO ==");

        // Configuração inicial da pontuação
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();

        // Validação crítica para garantir que a lista de palavras foi configurada no Inspector
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || allVowelData[vowelIndexToPlay].syllables.Count == 0)
        {
            Debug.LogError("ERRO CRÍTICO: 'All Vowel Data' não configurado ou 'vowelIndexToPlay' é inválido! O jogo não pode continuar.");
            return;
        }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        // Validação das referências de UI
        if (displayImage == null || micIndicatorImage == null)
        {
            Debug.LogError("ERRO CRÍTICO: Referências de UI (Display Image ou Mic Indicator Image) não atribuídas! O jogo não pode continuar.");
            return;
        }

        // Inicialização do serviço de voz e do fluxo do jogo
        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor);
        StartCoroutine(GameStartSequence());
    }

    /// <summary>
    /// Update é chamado uma vez por frame. Usado para debug com o teclado.
    /// </summary>
    private void Update()
    {
        // Ignora input do teclado se o jogo estiver processando um acerto/erro
        if (isProcessing) return;

        // Atalhos para forçar acerto (C) ou erro (X) durante a fase de escuta
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
        yield return new WaitForSeconds(initialDelay);
        CheckForMicrophonePermission();
        StartCoroutine(PlayTurnRoutine());
    }

    // VERSÃO REESCRITA PARA USAR O TRAINCONTROLLER
    private IEnumerator PlayTurnRoutine()
    {
        isProcessing = true;
        SetMicIndicator(promptingColor, false);

        // ETAPA 1: O TREM ENTRA COM A IMAGEM CORRETA
        Sprite spriteDaVez = currentSyllableList[currentIndex].image;
        yield return StartCoroutine(trainController.AnimateIn(spriteDaVez));

        // ETAPA 2: LOOP DE TENTATIVAS (O TREM FICA PARADO)
        bool pularPrompt = false;
        while (true)
        {
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

            // --- ALTERAÇÃO APLICADA AQUI ---
            if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
            
            // 1. Ativa a escuta de voz primeiro, de forma "invisível".
            receivedResult = false;
            isListening = true;
            SpeechToText.Start(this, true, false);

            // 2. Espera 1 segundo, com a escuta já ativa.
            yield return new WaitForSeconds(1f);

            // 3. Só agora o microfone fica verde para o usuário.
            SetMicIndicator(listeningColor, true);
            
            yield return new WaitUntil(() => receivedResult);
            isListening = false;
            SetMicIndicator(staticColor);
            // --- FIM DA ALTERAÇÃO ---

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
                break; // Acertou, sai do loop de tentativas
            }
            
            if (mistakeCount == 0) { pularPrompt = true; }
            mistakeCount++;
        }

        // ETAPA 3: FLUXO DE ACERTO
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
        }
        yield return new WaitForSeconds(delayAfterCorrect);

        // ETAPA 4: O TREM SAI
        yield return StartCoroutine(trainController.AnimateOut());
        
        // ETAPA 5: PREPARAÇÃO PARA O PRÓXIMO TURNO
        isProcessing = false;
        GoToNextImage();
    }
    #endregion
    
    #region Game Logic & Transitions

    /// <summary>
    /// Decide qual áudio tocar baseado no número de erros da rodada atual.
    /// </summary>
   // MÉTODO MODIFICADO COM AS REGRAS DO ZACA E A CORREÇÃO DO RANDOM
    private AudioClip GetCurrentPromptAudio()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];

        // --- VERIFICAÇÃO PARA "ZACA" ---
        if (mistakeCount == 0 && currentSyllable.word.ToLower() == "zaca")
        {
            List<AudioClip> zacaPrompts = new List<AudioClip>();
            if (zacaPrompt1 != null) zacaPrompts.Add(zacaPrompt1);
            if (zacaPrompt2 != null) zacaPrompts.Add(zacaPrompt2);

            if (zacaPrompts.Count > 0)
            {
                // CORREÇÃO: Usando UnityEngine.Random para ser explícito
                AudioClip chosenZacaPrompt = zacaPrompts[UnityEngine.Random.Range(0, zacaPrompts.Count)];
                Debug.Log($"[GetCurrentPromptAudio] - REGRA ESPECIAL 'ZACA' ATIVADA. Áudio: {chosenZacaPrompt.name}");
                return chosenZacaPrompt;
            }
        }

        AudioClip chosenClip;
        switch (mistakeCount)
        {
            case 0:
                // CORREÇÃO: Usando UnityEngine.Random para ser explícito
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
                    // CORREÇÃO: Usando UnityEngine.Random para ser explícito
                    chosenClip = supportAudios[UnityEngine.Random.Range(0, supportAudios.Count)];
                }
                else
                {
                    chosenClip = currentSyllable.hintFinalAudio;
                }
                break;
        }
        Debug.Log($"[GetCurrentPromptAudio] - Áudio escolhido: {(chosenClip != null ? chosenClip.name : "NENHUM")}");
        return chosenClip;
    }

    /// <summary>
    /// Prepara o jogo para a próxima imagem ou finaliza a fase se todas foram concluídas.
    /// </summary>
    private void GoToNextImage()
    {
        mistakeCount = 0; // Reseta o contador de erros para a nova imagem
        currentIndex++;
        Debug.Log("== [GoToNextImage] - Avançando para o índice: " + currentIndex + " ==");
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

    #region STT Interface & Utility Methods

    /// <summary>
    /// </summary>
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

    /// <summary>
    /// Define a cor e o estado de pulso do indicador do microfone.
    /// </summary>
    private void SetMicIndicator(Color color, bool shouldPulse = false)
    {
        if (micIndicatorImage != null) micIndicatorImage.color = color;
        if (micIndicatorAnimator != null) micIndicatorAnimator.SetBool("DevePulsar", shouldPulse);
    }

    /// <summary>
/// Compara a palavra esperada com a recebida usando um algoritmo de similaridade.
/// </summary>
private bool CheckMatch(string expected, string received)
{
    string normalizedExpected = RemoveAccents(expected);
    string normalizedReceived = RemoveAccents(received);

    // --- REGRAS ESPECIAIS ADICIONADAS ---

    // Regra para ZACA
    if (normalizedExpected == "zaca" && normalizedReceived == "vaca")
    {
        Debug.Log("--- REGRA ESPECIAL ATIVADA: 'vaca' aceito para 'zaca'. ---");
        return true;
    }

    // NOVA REGRA: Se esperar "pato" e ouvir "bato", aceita.
    if (normalizedExpected == "pato" && normalizedReceived == "bato")
    {
        Debug.Log("--- REGRA ESPECIAL ATIVADA: 'bato' aceito para 'pato'. ---");
        return true;
    }

    // NOVA REGRA: Se esperar "sapo" e ouvir "zapo", aceita.
    if (normalizedExpected == "sapo" && normalizedReceived == "zapo")
    {
        Debug.Log("--- REGRA ESPECIAL ATIVADA: 'zapo' aceito para 'sapo'. ---");
        return true;
    }
    
    // --- LÓGICA DE THRESHOLD INDIVIDUAL ADICIONADA AQUI ---
    // 1. Por padrão, usamos o threshold geral.
    float thresholdToUse = this.similarityThreshold;

    // 2. Se a palavra esperada for "zaca", trocamos para o threshold específico.
    if (normalizedExpected == "zaca")
    {
        thresholdToUse = this.zacaSimilarityThreshold;
        Debug.Log($"--- USANDO THRESHOLD ESPECÍFICO PARA ZACA: {thresholdToUse:P0} ---");
    }
    
    // O resto da lógica de comparação continua normalmente...
    Debug.Log($"--- COMPARANDO (SEM ACENTOS)! Esperado: '{normalizedExpected}' | Recebido: '{normalizedReceived}' ---");
    float similarity = 1.0f - ((float)LevenshteinDistance(normalizedExpected, normalizedReceived) / Mathf.Max(normalizedExpected.Length, received.Length));
    Debug.Log($"--- Similaridade: {similarity:P2} ---");

    // 3. A comparação final usa a variável 'thresholdToUse' que foi decidida acima.
    return similarity >= thresholdToUse;
}

    /// <summary>
    /// Calcula a Distância de Levenshtein entre duas strings (número de edições para igualá-las).
    /// </summary>
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

    /// <summary>
    /// Remove acentos de uma string para uma comparação mais flexível.
    /// </summary>
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

    /// <summary>
    /// Garante que o plugin de voz seja cancelado ao destruir o objeto para evitar erros.
    /// </summary>
    private void OnDestroy()
    {
        if (SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
        }
    }

    /// <summary>
    /// Controla o efeito de fade-in e fade-out da imagem principal.
    /// </summary>
    private IEnumerator FadeImage(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = displayImage.color.a;
        if (fadeIn) ShowImage(currentIndex);
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            displayImage.color = new Color(1, 1, 1, newAlpha);
            yield return null;
        }
        displayImage.color = new Color(1, 1, 1, targetAlpha);
    }

    private void ShowImage(int index)
    {
        if (index < 0 || index >= currentSyllableList.Count) return;
        displayImage.sprite = currentSyllableList[index].image;
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