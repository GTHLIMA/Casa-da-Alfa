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
    // === ESTRUTURA DE DADOS PARA CADA PALAVRA/SÍLABA ===
    [System.Serializable]
    public class SyllableData
    {
        public string word;             // A palavra que a criança deve falar.
        public Sprite image;            // A imagem correspondente.
        public AudioClip hintBasicAudio;    // Áudio da primeira dica.
        public AudioClip hintMediumAudio;   // Áudio da segunda dica.
        public AudioClip hintFinalAudio;    // Áudio da dica final (que revela a resposta).
    }

    // === ESTRUTURA PARA AGRUPAR AS SÍLABAS POR VOGAL ===
    [System.Serializable]
    public class VowelDataGroup
    {
        public string groupName;            // Nome para organização no Inspector (ex: "Vogal A").
        public List<SyllableData> syllables; // A lista de sílabas para este grupo.
    }

    // === CONFIGURAÇÕES GERAIS DA ATIVIDADE ===
    [Header("== Configuração Central da Atividade ==")]
    [Tooltip("Qual grupo de vogal usar da lista abaixo? (0 para o primeiro, 1 para o segundo, etc.)")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData; // Lista que contém todos os grupos de vogais.
    public string languageCode = "pt-BR";
    [Tooltip("Quão similar a palavra falada deve ser da esperada? (0.75 = 75%)")]
    [Range(0f, 1f)]
    public float similarityThreshold = 0.75f; // Limite para considerar a resposta como correta.

    // === REFERÊNCIAS DA INTERFACE GRÁFICA (UI) ===
    [Header("Referências da Interface (UI)")]
    public Image displayImage;
    public Image micIndicatorImage;
    public Animator micIndicatorAnimator;

    // === CORES PARA O INDICADOR VISUAL DO MICROFONE ===
    [Header("Cores do Indicador de Microfone")]
    public Color promptingColor = Color.red;    // Cor para quando o jogo está falando/dando dica.
    public Color listeningColor = Color.green;  // Cor para quando o jogo está ouvindo a criança.
    public Color staticColor = Color.white;     // Cor para momentos de transição ou processamento.

    // === ÁUDIOS DE FEEDBACK E INSTRUCIONAIS ===
    [Header("Áudios de Feedback")]
    public AudioClip standardPrompt;        // Pergunta padrão: "Que desenho é esse?"
    public List<AudioClip> variablePrompts; // Lista de perguntas que variam após as 3 primeiras imagens.
    public AudioClip congratulatoryAudio;   // Áudio de parabéns ao acertar.
    public List<AudioClip> supportAudios;     // Áudios motivacionais que alternam com a dica final.

    // === EFEITOS VISUAIS ===
    [Header("Efeitos Visuais")]
    public ParticleSystem endOfLevelConfetti;

    // === CONTROLES DE TEMPO DA ATIVIDADE ===
    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;       // Pausa inicial antes de tudo começar.
    public float delayAfterCorrect = 1.0f;  // Pausa após o áudio de parabéns e antes do fade.
    public float delayAfterHint = 1.5f;     // Pausa após uma pergunta ou dica, antes de começar a ouvir.
    public float fadeDuration = 0.5f;       // Duração do efeito de fade entre as imagens.

    // === VARIÁVEIS INTERNAS DE CONTROLE DO JOGO ===
    private List<SyllableData> currentSyllableList; // A lista de sílabas da vogal selecionada para jogar.
    private int currentIndex = 0;           // O índice da imagem/palavra atual.
    private int mistakeCount = 0;           // Contador de erros para a imagem atual.
    private bool receivedResult = false;        // Flag que sinaliza que o plugin de voz retornou um resultado.
    private string lastRecognizedText = "";     // O último texto que o plugin reconheceu.
    private int? lastErrorCode = null;          // O último código de erro do plugin.
    private bool isProcessing = false;          // Flag para bloquear ações enquanto uma rotina está em execução.
    private bool isListening = false;           // Flag para saber se estamos no estado de "escuta".
    private AudioManager audioManager;          // Referência para o seu gerenciador de áudio.

    // === VARIÁVEIS DE SCORE E MENUS ===
    [Header("========== Pause Menu & Score ==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;

    // Awake é chamado antes de qualquer método Start. Ideal para inicializações básicas.
    void Awake()
    {
        // Garante que o tempo do jogo não esteja pausado por causa de uma cena anterior.
        Time.timeScale = 1f;
        // Procura na cena por um objeto com a tag "Audio" para pegar a referência do AudioManager.
        audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
    }

    // Start é chamado uma vez no primeiro frame em que o script está ativo.
    void Start()
    {
        // Carrega a pontuação de uma cena anterior, se houver.
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();
        
        // Validação para garantir que as listas de palavras foram configuradas no Inspector.
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || allVowelData[vowelIndexToPlay].syllables.Count == 0) 
        {
            Debug.LogError("ERRO CRÍTICO: 'All Vowel Data' não configurado ou 'vowelIndexToPlay' é inválido! O jogo não pode continuar.");
            return;
        }
        // Define a lista de sílabas que será usada nesta rodada.
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;

        // Validação para garantir que as referências de UI foram arrastadas no Inspector.
        if (displayImage == null || micIndicatorImage == null) 
        {
            Debug.LogError("ERRO CRÍTICO: Referências de UI (Display Image ou Mic Indicator Image) não atribuídas! O jogo não pode continuar.");
            return;
        }

        // Inicializa o serviço de reconhecimento de voz.
        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor);
        
        // Inicia a corrotina que controla o fluxo de início do jogo.
        StartCoroutine(GameStartSequence());
    }

    // Update é chamado uma vez por frame. Usamos para debug com o teclado.
    void Update()
    {
        // Se o jogo está ocupado (processando um acerto/erro), ignora o input do teclado.
        if (isProcessing) return;

        // Se estamos no estado de "escuta", permite forçar um resultado.
        if (isListening)
        {
            // Pressionar 'C' simula um ACERTO.
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.LogWarning("--- DEBUG: Tecla C Pressionada -> Forçando Acerto ---");
                SpeechToText.Cancel(); // Para a escuta atual para evitar conflito.
                OnResultReceived(currentSyllableList[currentIndex].word, null);
            }
            // Pressionar 'X' simula um ERRO.
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.LogWarning("--- DEBUG: Tecla X Pressionada -> Forçando Erro ---");
                SpeechToText.Cancel(); // Para a escuta atual.
                OnResultReceived("palavra_errada_para_teste", null);
            }
        }
    }

    // Corrotina para a sequência inicial do jogo (delay + permissão).
    private IEnumerator GameStartSequence()
    {
        Debug.Log("[GameStartSequence] - Iniciando delay de " + initialDelay + " segundos.");
        yield return new WaitForSeconds(initialDelay);
        
        Debug.Log("[GameStartSequence] - Verificando permissão do microfone.");
        CheckForMicrophonePermission();

        Debug.Log("[GameStartSequence] - Sequência inicial completa. Começando o primeiro turno.");
        StartCoroutine(PlayTurnRoutine());
    }

    // O CORAÇÃO DO JOGO: gerencia um turno completo para uma imagem.
    private IEnumerator PlayTurnRoutine()
    {
        Debug.Log("== [PlayTurnRoutine] - INICIANDO NOVO TURNO para imagem #" + currentIndex + " ==");
        isProcessing = true; // Bloqueia o Update e outras ações.
        yield return StartCoroutine(FadeImage(true)); // Fade in da nova imagem.
        
        // Loop de tentativa e erro. Só sai deste loop quando a criança acerta.
        while (true)
        {
            // --- 1. FASE DA PERGUNTA/DICA ---
            Debug.Log("[PlayTurnRoutine] - Fase da Pergunta/Dica. Mic Vermelho.");
            SetMicIndicator(promptingColor);
            AudioClip promptClip = GetCurrentPromptAudio(); // Pega o áudio correto baseado nos erros.
            if (audioManager != null && promptClip != null)
            {
                audioManager.PlaySFX(promptClip);
                yield return new WaitForSeconds(promptClip.length + delayAfterHint);
            }
            else { yield return new WaitForSeconds(delayAfterHint); }

            // --- 2. FASE DE ESCUTA ---
            if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
            Debug.Log("[PlayTurnRoutine] - Fase de Escuta. Mic Verde.");
            SetMicIndicator(listeningColor, true);
            receivedResult = false;
            isListening = true;
            SpeechToText.Start(this, true, false);

            // Pausa a corrotina aqui e só continua quando OnResultReceived sinalizar que um resultado chegou.
            yield return new WaitUntil(() => receivedResult);
            isListening = false;
            SetMicIndicator(staticColor);

            // --- 3. PROCESSA O RESULTADO ---
            // Se houve erro no plugin ou não ouviu nada, conta como erro e tenta de novo.
            if (lastErrorCode.HasValue || string.IsNullOrEmpty(lastRecognizedText))
            {
                Debug.LogWarning("[PlayTurnRoutine] - Erro do plugin ou resultado vazio. Contando como erro.");
                mistakeCount++;
                continue; // Pula para a próxima iteração do loop, o que vai acionar a próxima dica.
            }

            // Compara a palavra ouvida com a esperada.
            string expectedWord = currentSyllableList[currentIndex].word.ToLower().Trim();
            string receivedWord = lastRecognizedText.ToLower().Trim();
            bool matched = CheckMatch(expectedWord, receivedWord);

            if (matched)
            {
                Debug.Log("[PlayTurnRoutine] - ACERTOU! Saindo do loop de tentativas.");
                yield return StartCoroutine(HandleCorrectAnswerFlow());
                break; // Sai do loop 'while(true)'.
            }
            else
            {
                Debug.LogWarning("[PlayTurnRoutine] - ERROU! Palavra não correspondeu.");
                mistakeCount++;
            }
        }
        
        // --- 4. PREPARA PARA A PRÓXIMA IMAGEM ---
        Debug.Log("[PlayTurnRoutine] - Fim do turno. Preparando para a próxima imagem.");
        yield return StartCoroutine(FadeImage(false));
        isProcessing = false; // Libera o processamento para o próximo turno.
        GoToNextImage();
    }
    
    // Decide qual áudio tocar baseado no número de erros.
    private AudioClip GetCurrentPromptAudio()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];
        Debug.Log($"[GetCurrentPromptAudio] - Verificando áudio para 'mistakeCount' = {mistakeCount}");
        switch (mistakeCount)
        {
            case 0: // 1ª Tentativa (sem erro)
                if (currentIndex < 3) return standardPrompt;
                else {
                    if (variablePrompts != null && variablePrompts.Count > 0) return variablePrompts[UnityEngine.Random.Range(0, variablePrompts.Count)];
                    else return standardPrompt;
                }
            case 1: return standardPrompt;
            case 2: return currentSyllable.hintBasicAudio;
            case 3: return currentSyllable.hintMediumAudio;
            case 4: return currentSyllable.hintFinalAudio;
            default: // A partir do 4º erro, alterna.
                if (mistakeCount % 2 != 0) { // Erros ímpares (5º, 7º...)
                    if (supportAudios != null && supportAudios.Count > 0) return supportAudios[UnityEngine.Random.Range(0, supportAudios.Count)];
                    else return currentSyllable.hintFinalAudio;
                } else { // Erros pares (6º, 8º...)
                    return currentSyllable.hintFinalAudio;
                }
        }
    }

    // Prepara para a próxima imagem.
    void GoToNextImage()
    {
        mistakeCount = 0; // Reseta os erros para a nova imagem.
        currentIndex++;
        Debug.Log("== [GoToNextImage] - Avançando para o índice: " + currentIndex + " ==");
        if (currentIndex >= currentSyllableList.Count)
        {
            ShowEndPhasePanel(); // Se não houver mais imagens, termina o jogo.
        }
        else
        {
            StartCoroutine(PlayTurnRoutine()); // Se houver, começa o próximo turno.
        }
    }

    // Corrotina para o fluxo de acerto.
    private IEnumerator HandleCorrectAnswerFlow()
    {
        Debug.Log("== [HandleCorrectAnswerFlow] - Fluxo de ACERTO iniciado. ==");
        SetMicIndicator(staticColor, false);
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
            yield return new WaitForSeconds(congratulatoryAudio.length);
        }
        yield return new WaitForSeconds(delayAfterCorrect);
    }
    
    // Define a cor do indicador do microfone.
    void SetMicIndicator(Color color, bool shouldPulse = false)
    {
        if (micIndicatorImage != null) micIndicatorImage.color = color;
        if (micIndicatorAnimator != null) micIndicatorAnimator.SetBool("DevePulsar", shouldPulse);
    }
    
    // Método da interface que é chamado pelo plugin de voz quando um resultado é obtido.
    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        Debug.Log($"<<<<< [OnResultReceived] - PLUGIN RETORNOU! Texto: '{recognizedText}', Código de Erro: {(errorCode.HasValue ? errorCode.Value.ToString() : "Nenhum")} >>>>>");
        if (isProcessing)
        {
            isListening = false;
            lastRecognizedText = recognizedText;
            lastErrorCode = errorCode;
            receivedResult = true; // Sinaliza para a corrotina principal continuar.
        }
        else
        {
            Debug.LogWarning("[OnResultReceived] - Resultado recebido, mas o jogo não estava em modo de escuta (isProcessing=false). Ignorando.");
        }
    }
    
    #region Métodos de Interface e Auxiliares
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
    public void ShowEndPhasePanel() { Debug.Log("== [ShowEndPhasePanel] - FIM DE JOGO! =="); if (endPhasePanel != null) endPhasePanel.SetActive(true); if (audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3); if (endOfLevelConfetti != null) endOfLevelConfetti.Play(); UpdateAllScoreDisplays(); }
    public void AddScore(int amount) { score += amount; if (score < 0) score = 0; if (numberCounter != null) numberCounter.Value = score; ScoreTransfer.Instance?.SetScore(score); UpdateAllScoreDisplays(); }
    void UpdateAllScoreDisplays() { string formattedScore = score.ToString("000"); if (scoreHUD != null) scoreHUD.text = formattedScore; if (scorePause != null) scorePause.text = "Score: " + formattedScore; if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + formattedScore; }
    #endregion
}