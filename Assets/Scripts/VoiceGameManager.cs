using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ImageVoiceMatcher : MonoBehaviour, ISpeechToTextListener
{
    [System.Serializable]
    public class WordData
    {
        public string word;
        public Sprite image;
        // public AudioClip wordAudio; // Opcional
    }

    [Header("Configuração da Atividade")]
    public List<WordData> wordList;
    public string languageCode = "pt-BR";

    [Header("Referências da Interface (UI)")]
    public Image displayImage;
    public TMP_Text feedbackText;
    public Button listenButton;

    [Header("Mensagens de Feedback")]
    public string correctMessage = "Muito bem! ✅";
    public string tryAgainMessage = "Quase lá! Tente de novo. ❌";
    public string listeningMessage = "Ouvindo... 🎤";
    public string initialMessage = "Pressione 'Ouvir' e diga o nome!";
    public string explanationInProgressMessage = "Escute a explicação...";
    public string permissionNeededMessage = "Precisamos da sua permissão para usar o microfone!";
    public string permissionDeniedMessage = "Permissão negada! Habilite o microfone para este app nas configurações do seu celular.";
    public float delayAfterCorrect = 1.0f;

    [Header("Áudios da Atividade")]
    public AudioClip explanationAudio;
    public AudioClip congratulatoryAudio;
    public AudioClip tryAgainAudio;

    private int currentIndex = 0;
    private bool isListening = false;
    private bool isProcessing = false;
    private bool explanationFinished = false; // Nova flag

    [Header("========== Pause Menu & Score ==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;
    private AudioManager audioManager;

    void Awake()
    {
        Time.timeScale = 1f;
        Debug.Log("ImageVoiceMatcher: Awake() -> Time.timeScale definido para 1f.");

        GameObject amObject = GameObject.FindGameObjectWithTag("Audio");
        if (amObject != null) audioManager = amObject.GetComponent<AudioManager>();
        else Debug.LogError("ImageVoiceMatcher: AudioManager não encontrado! Verifique a tag 'Audio'.");
    }

    void Start()
    {
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;
        UpdateAllScoreDisplays();

        if (wordList == null || wordList.Count == 0)
        {
            if (feedbackText != null) feedbackText.text = "ERRO: Nenhuma palavra configurada!";
            Debug.LogError("ImageVoiceMatcher: A lista 'wordList' está vazia ou nula!");
            if (listenButton != null) listenButton.interactable = false;
            return;
        }

        if (displayImage == null || feedbackText == null)
        {
            Debug.LogError("ImageVoiceMatcher: Referências de UI (displayImage ou feedbackText) não configuradas!");
            if (listenButton != null) listenButton.interactable = false;
            return;
        }

        SpeechToText.Initialize(languageCode);
        Debug.Log("ImageVoiceMatcher: SpeechToText Inicializado com idioma: " + languageCode);

        if (listenButton != null)
        {
            listenButton.onClick.AddListener(OnListenButtonPressed);
            listenButton.interactable = false;
        }

        ShowImage(currentIndex);
        if (feedbackText != null) feedbackText.text = explanationInProgressMessage;

        StartCoroutine(PlayExplanationAndEnableGame());
        CheckAndRequestPermission(); // Pede permissão enquanto a explicação pode estar tocando
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) // DEBUG para simular acerto
        {
            if (isListening || isProcessing) return;
            if (wordList == null || wordList.Count == 0 || currentIndex >= wordList.Count) return;
            Debug.Log("DEBUG: Tecla C pressionada, simulando acerto para: '" + wordList[currentIndex].word + "'");
            OnResultReceived(wordList[currentIndex].word, null);
        }
    }

    private IEnumerator PlayExplanationAndEnableGame()
    {
        if (listenButton != null) listenButton.interactable = false;

        if (audioManager != null && explanationAudio != null)
        {
            Debug.Log("ImageVoiceMatcher: Tocando áudio de explicação...");
            audioManager.PlaySFX(explanationAudio);
            yield return new WaitForSeconds(explanationAudio.length);
            Debug.Log("ImageVoiceMatcher: Áudio de explicação terminado.");
        }
        else
        {
            Debug.LogWarning("ImageVoiceMatcher: Áudio de explicação ou AudioManager não configurado.");
            yield return new WaitForSeconds(0.5f);
        }
        explanationFinished = true;
        TryEnableListenButton(); // Tenta habilitar o botão
    }

    void CheckAndRequestPermission()
    {
        if (!SpeechToText.CheckPermission())
        {
            if (feedbackText != null) feedbackText.text = permissionNeededMessage;
            Debug.Log("ImageVoiceMatcher: Pedindo permissão de microfone...");
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    Debug.Log("ImageVoiceMatcher: Permissão de microfone concedida!");
                    TryEnableListenButton(); // Tenta habilitar o botão
                }
                else
                {
                    if (feedbackText != null) feedbackText.text = permissionDeniedMessage;
                    Debug.LogError("ImageVoiceMatcher: Permissão de microfone negada!");
                    if (listenButton != null) listenButton.interactable = false;
                }
            });
        }
        else
        {
            Debug.Log("ImageVoiceMatcher: Permissão de microfone já concedida.");
            TryEnableListenButton(); // Tenta habilitar o botão
        }
    }

    // Novo método auxiliar para habilitar o botão de ouvir
    void TryEnableListenButton()
    {
        // Só habilita se a explicação terminou E tem permissão
        if (listenButton != null && explanationFinished && SpeechToText.CheckPermission())
        {
            listenButton.interactable = true;
            if (feedbackText != null && (feedbackText.text == explanationInProgressMessage || feedbackText.text == permissionNeededMessage))
            {
                feedbackText.text = initialMessage; // Define a mensagem inicial após tudo estar pronto
            }
            Debug.Log("ImageVoiceMatcher: Botão de ouvir HABILITADO.");
        }
        else if(listenButton != null)
        {
            Debug.Log("ImageVoiceMatcher: Botão de ouvir NÃO habilitado ainda. Explicação Finalizada: " + explanationFinished + ", Permissão: " + SpeechToText.CheckPermission());
        }
    }


    void OnListenButtonPressed()
    {
        if (!SpeechToText.CheckPermission())
        {
            Debug.LogWarning("ImageVoiceMatcher: OnListenButtonPressed - Sem permissão. Pedindo novamente.");
            CheckAndRequestPermission();
            return;
        }

        if (isListening || isProcessing)
        {
            Debug.LogWarning("ImageVoiceMatcher: OnListenButtonPressed - Bloqueado (isListening=" + isListening + " ou isProcessing=" + isProcessing + ")");
            return;
        }

        if (feedbackText != null) feedbackText.text = listeningMessage;
        isListening = true;
        if (listenButton != null) listenButton.interactable = false;

        Debug.Log("OnListenButtonPressed: Chamando SpeechToText.Start. Time.timeScale: " + Time.timeScale);
        bool started = SpeechToText.Start(this, true, false);

        if (!started)
        {
            if (feedbackText != null) feedbackText.text = "Erro ao iniciar a escuta.";
            Debug.LogError("ImageVoiceMatcher: SpeechToText.Start falhou em iniciar.");
            isListening = false;
            isProcessing = false;
            TryEnableListenButton(); // Tenta reabilitar
        }
        else
        {
            Debug.Log("ImageVoiceMatcher: Escuta iniciada via botão...");
        }
    }

    void ShowImage(int index)
    {
        if (index < 0 || index >= wordList.Count)
        {
            Debug.LogError("ImageVoiceMatcher: ShowImage - Índice inválido: " + index);
            return;
        }
        if (displayImage == null)
        {
            Debug.LogError("ImageVoiceMatcher: displayImage não está atribuído!");
            return;
        }
        displayImage.sprite = wordList[index].image;
        displayImage.color = Color.white;
        displayImage.preserveAspect = true;
        Debug.Log("ImageVoiceMatcher: ShowImage - Mostrando imagem: '" + wordList[index].word + "' (Índice: " + index + ")");
    }

    void GoToNextImage()
    {
        Debug.Log("GoToNextImage: CHAMADO. currentIndex ANTES: " + currentIndex);
        currentIndex++;
        Debug.Log("GoToNextImage: currentIndex DEPOIS: " + currentIndex + " | Total: " + wordList.Count);

        if (currentIndex >= wordList.Count)
        {
            Debug.Log("GoToNextImage: FIM DA LISTA.");
            if (feedbackText != null) feedbackText.text = "🎉 Parabéns! Você completou todas as imagens! 🎉";
            if (displayImage != null) displayImage.enabled = false; // Esconde a imagem
            ShowEndPhasePanel();
            if (listenButton != null) listenButton.interactable = false;
        }
        else
        {
            Debug.Log("GoToNextImage: MOSTRANDO PRÓXIMA IMAGEM.");
            ShowImage(currentIndex);
            if (feedbackText != null) feedbackText.text = initialMessage;
            TryEnableListenButton(); // Habilita o botão para a próxima palavra
        }
    }

    private IEnumerator HandleCorrectAnswerFlow()
    {
        // isProcessing já é true
        Debug.Log("HandleCorrectAnswerFlow: Iniciado. Time.timeScale: " + Time.timeScale);

        if (audioManager != null && congratulatoryAudio != null)
        {
            audioManager.PlaySFX(congratulatoryAudio);
            yield return new WaitForSeconds(congratulatoryAudio.length);
        }
        yield return new WaitForSeconds(delayAfterCorrect);
        GoToNextImage();
        isProcessing = false; // Libera após tudo
        Debug.Log("ImageVoiceMatcher: Fluxo de acerto concluído, isProcessing = false.");
    }

    private IEnumerator HandleWrongAnswerOrErrorFlow()
    {
        isListening = false; // Garante que não está mais ouvindo
        isProcessing = true; // Marca que está processando o feedback de erro
        if (listenButton != null) listenButton.interactable = false;
        Debug.Log("HandleWrongAnswerOrErrorFlow: Iniciado. Botão desabilitado.");

        if (audioManager != null && tryAgainAudio != null)
        {
            audioManager.PlaySFX(tryAgainAudio);
            yield return new WaitForSeconds(tryAgainAudio.length);
        } else {
            yield return new WaitForSeconds(1.5f); // Delay padrão
        }

        isProcessing = false; // Libera processamento
        TryEnableListenButton(); // Tenta reabilitar o botão
        Debug.Log("ImageVoiceMatcher: Fluxo de erro/tentativa concluído, isProcessing = false.");
    }

    // --- MÉTODOS DA INTERFACE ISpeechToTextListener ---
    public void OnReadyForSpeech()
    {
        Debug.Log("ImageVoiceMatcher STT: OnReadyForSpeech - Pronto para ouvir. Time.timeScale: " + Time.timeScale);
    }

    public void OnBeginningOfSpeech()
    {
        Debug.Log("ImageVoiceMatcher STT: OnBeginningOfSpeech - Usuário começou a falar. Time.timeScale: " + Time.timeScale);
    }

    public void OnVoiceLevelChanged(float level)
    {
        // Exemplo: Debug.Log("ImageVoiceMatcher STT: Nível da voz: " + level);
    }

    public void OnPartialResultReceived(string partialText)
    {
        // Exemplo: Debug.Log("ImageVoiceMatcher STT: Resultado parcial: " + partialText);
    }

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        Debug.Log("ImageVoiceMatcher STT: OnResultReceived - Texto: '" + recognizedText + "', Código de Erro: " + (errorCode.HasValue ? errorCode.Value.ToString() : "Nenhum") + " | Time.timeScale INÍCIO: " + Time.timeScale);
        isListening = false;

        if (isProcessing)
        {
            Debug.LogWarning("ImageVoiceMatcher STT: Resultado recebido, mas já estava processando (isProcessing=true). Ignorando este.");
            return;
        }

        if (errorCode.HasValue)
        {
            string friendlyErrorMessage = GetFriendlyErrorMessage(errorCode.Value);
            Debug.LogError($"ImageVoiceMatcher STT: Erro de reconhecimento - Código {errorCode.Value}. Mensagem: {friendlyErrorMessage}");
            if (feedbackText != null) feedbackText.text = friendlyErrorMessage;
            StartCoroutine(HandleWrongAnswerOrErrorFlow());
            return;
        }

        if (string.IsNullOrEmpty(recognizedText) && !errorCode.HasValue) // Adicionado !errorCode.HasValue para ter certeza
        {
            Debug.LogWarning("ImageVoiceMatcher STT: OnResultReceived - Resultado vazio recebido (sem erro de plugin).");
            if (feedbackText != null) feedbackText.text = tryAgainMessage + "\n(Não ouvi nada)";
            StartCoroutine(HandleWrongAnswerOrErrorFlow());
            return;
        }
        
        isProcessing = true;

        string expectedWord = wordList[currentIndex].word.ToLower().Trim();
        string receivedWord = recognizedText.ToLower().Trim();

        Debug.Log($"ImageVoiceMatcher: OnResultReceived - Comparando... Esperado: '{expectedWord}', Recebido: '{receivedWord}'");

        bool matched = false;
        if (expectedWord == "zaca")
        {
            if (receivedWord.Contains("zaca") || receivedWord.Contains("saca") || receivedWord.Contains("zacka") ||
                receivedWord.Contains("za ca") || receivedWord.Contains("sa ca") ||
                receivedWord.Contains("chaca") || receivedWord.Contains("caca"))
            {
                Debug.Log("ImageVoiceMatcher: Match especial para 'ZACA' bem-sucedido com '" + receivedWord + "'");
                matched = true;
            }
        }
        else
        {
            if (receivedWord.Contains(expectedWord))
            {
                matched = true;
            }
        }

        if (matched)
        {
            if (feedbackText != null) feedbackText.text = correctMessage;
            Debug.Log("ImageVoiceMatcher: ACERTOU!");
            AddScore(10);
            StartCoroutine(HandleCorrectAnswerFlow());
        }
        else 
        {
            if (feedbackText != null)
            {
                
                feedbackText.text = tryAgainMessage + $"\n(Você disse: {receivedWord})";
            }
            Debug.Log($"ImageVoiceMatcher: ERROU! Esperado: '{expectedWord}', Recebido: '{receivedWord}'");
            StartCoroutine(HandleWrongAnswerOrErrorFlow());
        }
    }
    private string GetFriendlyErrorMessage(int errorCode)
    {
        // ... (seu código GetFriendlyErrorMessage) ...
        Debug.Log("GetFriendlyErrorMessage chamado com código: " + errorCode);
        switch (errorCode)
        {
            case 0: return "Escuta cancelada."; // Pode acontecer se SpeechToText.Cancel() for chamado
            case 1: return "Problema de rede. Verifique sua conexão e tente de novo.";
            case 2: return "Erro de conexão. Tente novamente.";
            case 3: return "Erro de áudio. Verifique seu microfone.";
            case 4: return "Erro no servidor de reconhecimento. Tente mais tarde.";
            case 5: return "Ocorreu um problema. Tente de novo.";
            case 6: // SpeechRecognizer.ERROR_SPEECH_TIMEOUT ou ERROR_NO_MATCH
            case 7: // SpeechRecognizer.ERROR_NO_MATCH (alguns plugins podem retornar 7)
                return "Não ouvi nada ou não entendi. Fale mais alto e claro, por favor.";
            case 8: return "O serviço de voz está ocupado. Tente em alguns segundos.";
            case 9: return "O app Google precisa de permissão para usar o microfone. Verifique as configurações.";
            default: return $"Não entendi. Tente de novo. (Erro {errorCode})";
        }
    }

    void OnDestroy()
    {
        if (listenButton != null)
        {
            listenButton.onClick.RemoveListener(OnListenButtonPressed);
        }
        if (SpeechToText.IsBusy())
        {
            Debug.Log("ImageVoiceMatcher: OnDestroy - Cancelando escuta pendente do SpeechToText.");
            SpeechToText.Cancel();
        }
    }

    #region Pause Menu and Score Management
    public void ClosePauseMenu()
    {
        PauseMenu.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log("ImageVoiceMatcher ClosePauseMenu: Time.timeScale = " + Time.timeScale);
    }

    public void OpenPauseMenu()
    {
        Debug.Log("ImageVoiceMatcher OpenPauseMenu FOI CHAMADO!");
        if (scorePause != null) scorePause.text = "Score: " + score.ToString("000");
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
        ScoreTransfer.Instance?.SetScore(score);
        Debug.Log("ImageVoiceMatcher OpenPauseMenu: Time.timeScale = " + Time.timeScale);
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("ImageVoiceMatcher ShowEndPhasePanel: CHAMADO. Score: " + score);
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString("000");
        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        ScoreTransfer.Instance?.SetScore(score);
        if (audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3);
        // Considerar pausar o jogo aqui se desejar que nada mais aconteça
        // Time.timeScale = 0f;
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        if (numberCounter != null) numberCounter.Value = score;
        ScoreTransfer.Instance?.SetScore(score);
        UpdateAllScoreDisplays();
        Debug.Log("ImageVoiceMatcher AddScore: Pontuação atualizada para: " + score);
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