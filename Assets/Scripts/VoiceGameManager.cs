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
        public AudioClip hintBasicAudio;    // Dica para o 2º erro
        public AudioClip hintMediumAudio;   // Dica para o 3º erro
        public AudioClip hintFinalAudio;    // Dica para o 4º erro (diz a resposta)
    }

    [System.Serializable]
    public class VowelDataGroup { public string groupName; public List<SyllableData> syllables; }

    [Header("== Configuração Central da Atividade ==")]
    public int vowelIndexToPlay = 0;
    public List<VowelDataGroup> allVowelData;
    public string languageCode = "pt-BR";

    [Header("Referências da Interface (UI)")]
    public Image displayImage;
    public Image micIndicatorImage;
    public Animator listenButtonAnimator;

    [Header("Cores do Indicador de Microfone")]
    public Color promptingColor = Color.red;
    public Color listeningColor = Color.green;
    public Color staticColor = Color.white;

    [Header("Áudios de Feedback")]
    public AudioClip initialPromptAudio;    // "Que desenho é esse?"
    public List<AudioClip> variablePrompts;
    public AudioClip congratulatoryAudio;
    public List<AudioClip> supportAudios; // "Tente novamente", "Você quase acertou!"

    [Header("Efeitos Visuais")]
    public ParticleSystem endOfLevelConfetti;

    [Header("Controles de Tempo")]
    public float initialDelay = 2.0f;
    public float delayAfterCorrect = 1.0f;
    public float delayAfterHint = 1.5f;
    public float fadeDuration = 0.5f;

    // --- Variáveis Internas ---
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
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
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
        // Setup do Score
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();
        
        // Validação da atividade
        if (allVowelData == null || allVowelData.Count <= vowelIndexToPlay || allVowelData[vowelIndexToPlay].syllables.Count == 0) { Debug.LogError("ERRO: Configuração da lista de palavras ('All Vowel Data') inválida!"); return; }
        currentSyllableList = allVowelData[vowelIndexToPlay].syllables;
        if (displayImage == null || micIndicatorImage == null) { Debug.LogError("ERRO: Referências de UI (Display Image ou Mic Indicator Image) não atribuídas!"); return; }

        SpeechToText.Initialize(languageCode);
        SetMicIndicator(staticColor, false);
        StartCoroutine(GameStartSequence());
    }

    void Update()
    {
        // Se já estiver processando um acerto ou erro, não permite novas ações de debug.
        if (isProcessing) return;

        // Se estiver no estado de "escuta", permite forçar um resultado
        if (isListening)
        {
            // Pressionar 'C' simula um ACERTO
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.LogWarning("--- DEBUG: Tecla C Pressionada -> Forçando Acerto ---");
                SpeechToText.Cancel(); // Para a escuta atual
                OnResultReceived(currentSyllableList[currentIndex].word, null);
            }

            // Pressionar 'X' simula um ERRO
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.LogWarning("--- DEBUG: Tecla X Pressionada -> Forçando Erro ---");
                SpeechToText.Cancel(); // Para a escuta atual
                OnResultReceived("palavra_errada_para_teste", null);
            }
        }
        
        // --- NOVO: Tecla 'N' para PULAR para a próxima imagem a qualquer momento ---
        // Funciona mesmo que não esteja no modo de escuta, contanto que não esteja processando.
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.LogWarning("--- DEBUG: Tecla N Pressionada -> Forçando avanço para a próxima imagem ---");

            // Se estiver ouvindo, cancela primeiro.
            if (isListening)
            {
                SpeechToText.Cancel();
                isListening = false;
            }

            // Inicia o fluxo de acerto, que levará à próxima imagem.
            StartCoroutine(HandleCorrectAnswerFlow());
        }
    }

   
    private IEnumerator GameStartSequence()
    {
        yield return new WaitForSeconds(initialDelay);
        CheckForMicrophonePermission();
        StartCoroutine(PlayTurnRoutine());
    }

   private IEnumerator PlayTurnRoutine()
    {
        isProcessing = true;
        yield return StartCoroutine(FadeImage(true));
        
        while (true)
        {
            SetMicIndicator(promptingColor, false);
            // MODIFICADO: Usa o novo método para pegar a pergunta certa
            AudioClip promptClip = GetCurrentQuestionAudio(); 
            if (audioManager != null && promptClip != null)
            {
                audioManager.PlaySFX(promptClip);
                yield return new WaitForSeconds(promptClip.length);
            }
            else { yield return new WaitForSeconds(delayAfterHint); }

            if (!SpeechToText.CheckPermission()) { isProcessing = false; yield break; }
            
            SetMicIndicator(listeningColor, true);
            receivedResult = false;
            isListening = true;
            SpeechToText.Start(this, true, false);

            yield return new WaitUntil(() => receivedResult);
            isListening = false;
            SetMicIndicator(staticColor, false);

            if (lastErrorCode.HasValue || string.IsNullOrEmpty(lastRecognizedText))
            {
                mistakeCount++;
                continue;
            }

            string expectedWord = currentSyllableList[currentIndex].word.ToLower().Trim();
            string receivedWord = lastRecognizedText.ToLower().Trim();
            bool matched = CheckMatch(expectedWord, receivedWord);

            if (matched)
            {
                yield return StartCoroutine(HandleCorrectAnswerFlow());
                break;
            }
            else
            {
                mistakeCount++;
            }
        }
        
        yield return StartCoroutine(FadeImage(false));
        isProcessing = false;
        GoToNextImage();
    }

    // --- NOVO MÉTODO PARA DECIDIR QUAL PERGUNTA TOCAR ---
    private AudioClip GetCurrentQuestionAudio()
    {
        // Se for uma das 3 primeiras figuras (índice 0, 1, 2)
        if (currentIndex < 3)
        {
            Debug.Log("Usando pergunta padrão para a imagem #" + currentIndex);
            return initialPromptAudio;
        }
        else // A partir da 4ª figura
        {
            if (variablePrompts != null && variablePrompts.Count > 0)
            {
                // Sorteia uma pergunta da lista de perguntas variadas
                int randomIndex = Random.Range(0, variablePrompts.Count);
                Debug.Log("Usando pergunta variada #" + randomIndex + " para a imagem #" + currentIndex);
                return variablePrompts[randomIndex];
            }
            else
            {
                // Se a lista estiver vazia, usa a padrão como segurança
                Debug.LogWarning("Lista de perguntas variadas está vazia! Usando a pergunta padrão.");
                return initialPromptAudio;
            }
        }
    }

    // --- MÉTODO COM A LÓGICA DE DICAS ATUALIZADA ---
    private AudioClip GetCurrentPromptAudio()
    {
        SyllableData currentSyllable = currentSyllableList[currentIndex];
        switch (mistakeCount)
        {
            case 0: // 1ª Tentativa (sem erro)
                return initialPromptAudio; // "Que desenho é esse?"
            case 1: // 2ª Tentativa (após 1º erro) - silencioso, apenas repete a pergunta
                return initialPromptAudio;
            case 2: // 3ª Tentativa (após 2º erro)
                return currentSyllable.hintBasicAudio;
            case 3: // 4ª Tentativa (após 3º erro)
                return currentSyllable.hintMediumAudio;
            case 4: // 5ª Tentativa (após 4º erro)
                return currentSyllable.hintFinalAudio;
            default: // A partir da 6ª tentativa...
                // ...alterna entre o áudio motivacional e a dica final.
                if (mistakeCount % 2 != 0) // Em erros ímpares (5º, 7º, ...)
                {
                    if (supportAudios != null && supportAudios.Count > 0)
                        return supportAudios[Random.Range(0, supportAudios.Count)];
                    else 
                        return currentSyllable.hintFinalAudio; // Segurança
                }
                else // Em erros pares (6º, 8º, ...)
                {
                    return currentSyllable.hintFinalAudio;
                }
        }
    }

    void GoToNextImage()
    {
        mistakeCount = 0;
        currentIndex++;
        if (currentIndex >= currentSyllableList.Count)
        {
            ShowEndPhasePanel();
        }
        else
        {
            StartCoroutine(PlayTurnRoutine());
        }
    }

    private IEnumerator HandleCorrectAnswerFlow()
    {
        SetMicIndicator(staticColor, false);
        AddScore(10);
        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
            yield return new WaitForSeconds(congratulatoryAudio.length);
        }
        yield return new WaitForSeconds(delayAfterCorrect);
    }
    
    // Este método de erro agora só é usado pela tecla de debug 'X'
    private IEnumerator HandleWrongAnswerOrErrorFlow()
    {
        SetMicIndicator(staticColor, false);
        Debug.LogWarning("Fluxo de erro forçado pela tecla 'X'.");
        mistakeCount++;
        // Na lógica principal, o loop do PlayTurnRoutine cuidará do resto.
        // Para o debug, apenas esperamos e liberamos.
        isProcessing = true;
        yield return new WaitForSeconds(1.5f);
        isProcessing = false;
        // Não reabilitamos o botão, pois o loop principal não está rodando no debug.
    }
    
    void SetMicIndicator(Color color, bool shouldPulse) { if (micIndicatorImage != null) micIndicatorImage.color = color; if (listenButtonAnimator != null) listenButtonAnimator.SetBool("DevePulsar", shouldPulse); }
    public void OnResultReceived(string recognizedText, int? errorCode) { isListening = false; if (isProcessing) { receivedResult = true; return; } lastRecognizedText = recognizedText; lastErrorCode = errorCode; receivedResult = true; }
    
    #region Métodos de Interface e Auxiliares
    public void OnReadyForSpeech() {}
    public void OnBeginningOfSpeech() {}
    public void OnVoiceLevelChanged(float level) {}
    public void OnPartialResultReceived(string partialText) {}
    private bool CheckMatch(string expected, string received) { if (expected == "zaca") return received.Contains("zaca") || received.Contains("saca"); else return received.Contains(expected); }
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