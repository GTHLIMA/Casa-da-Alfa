using UnityEngine;
using UnityEngine.UI; // Necessário para Image
using TMPro;        // Necessário para TextMeshProUGUI
using System.Collections;
using System.Collections.Generic;

// Certifique-se de que a interface ISpeechToTextListener e a classe SpeechToText
// fazem parte do plugin que você está usando.
public class ImageVoiceMatcher : MonoBehaviour, ISpeechToTextListener
{
    [System.Serializable]
    public class WordData
    {
        public string word;
        public Sprite image;
        // Opcional: Adicionar um clipe de áudio para a palavra
        // public AudioClip wordAudio;
    }

    [Header("Configuração da Atividade")]
    public List<WordData> wordList;
    public string languageCode = "pt-BR";

    [Header("Referências da Interface (UI)")]
    public Image displayImage;
    public TMP_Text feedbackText;
    public Button listenButton;

    [Header("Feedback Messages")]
    public string correctMessage = "Muito bem! ✅";
    public string tryAgainMessage = "Quase lá! Tente de novo. ❌";
    public string listeningMessage = "Ouvindo... 🎤";
    public string initialMessage = "Pressione 'Ouvir' e diga o nome!";
    public float delayAfterCorrect = 1.5f;

    private int currentIndex = 0;
    private bool isListening = false;
    private bool isProcessing = false;

    [Header("========== Pause Menu & Score ==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;
    private AudioManager audioManager; // Inicialize em Awake ou Start se for usar

    void Start()
    {
        // Pega referência do AudioManager
        // Se você não tiver um AudioManager na cena com a tag "Audio", isso pode dar erro.
        // audioManager = GameObject.FindGameObjectWithTag("Audio")?.GetComponent<AudioManager>();
        // É mais seguro atribuir via Inspector se possível, ou garantir que ele exista.
        GameObject amObject = GameObject.FindGameObjectWithTag("Audio");
        if (amObject != null) audioManager = amObject.GetComponent<AudioManager>();


        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;

        UpdateAllScoreDisplays(); // Atualiza todos os textos de score

        if (wordList == null || wordList.Count == 0)
        {
            feedbackText.text = "ERRO: Nenhuma palavra configurada!";
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
        }

        ShowImage(currentIndex);
        feedbackText.text = initialMessage;

        CheckAndRequestPermission();
    }

    // DEBUG: Simular acerto com a tecla 'C' para testar no editor
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isListening || isProcessing)
            {
                Debug.LogWarning("DEBUG: Tecla C pressionada, mas está ouvindo/processando. Simulação ignorada.");
                return;
            }
            if (wordList == null || wordList.Count == 0 || currentIndex >= wordList.Count)
            {
                 Debug.LogWarning("DEBUG: Tecla C pressionada, mas wordList está inválida ou fim da lista. Simulação ignorada.");
                return;
            }

            Debug.Log("DEBUG: Tecla C pressionada, simulando acerto para: '" + wordList[currentIndex].word + "'");
            // Simula um resultado correto do SpeechToText
            OnResultReceived(wordList[currentIndex].word, null);
        }
    }

    void CheckAndRequestPermission()
    {
        if (!SpeechToText.CheckPermission())
        {
            feedbackText.text = "Pedindo permissão de microfone...";
            Debug.Log("ImageVoiceMatcher: Pedindo permissão de microfone...");
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    feedbackText.text = initialMessage;
                    Debug.Log("ImageVoiceMatcher: Permissão de microfone concedida!");
                    if (listenButton != null) listenButton.interactable = true;
                }
                else
                {
                    feedbackText.text = "Permissão negada! Habilite o microfone para este app nas configurações do seu celular.";
                    Debug.LogError("ImageVoiceMatcher: Permissão de microfone negada!");
                    if (listenButton != null) listenButton.interactable = false;
                }
            });
        }
        else
        {
            Debug.Log("ImageVoiceMatcher: Permissão de microfone já concedida.");
            if (listenButton != null) listenButton.interactable = true;
        }
    }

    void OnListenButtonPressed()
    {
        if (isListening || isProcessing)
        {
            Debug.LogWarning("ImageVoiceMatcher: OnListenButtonPressed - Tentativa de iniciar escuta enquanto isListening=" + isListening + " ou isProcessing=" + isProcessing);
            return;
        }

        if (!SpeechToText.CheckPermission())
        {
            Debug.LogWarning("ImageVoiceMatcher: OnListenButtonPressed - Sem permissão de microfone. Tentando pedir novamente.");
            CheckAndRequestPermission();
            return;
        }

        feedbackText.text = listeningMessage;
        isListening = true;
        if (listenButton != null) listenButton.interactable = false;

        bool started = SpeechToText.Start(this, true, false);

        if (!started)
        {
            feedbackText.text = "Erro ao iniciar a escuta.";
            Debug.LogError("ImageVoiceMatcher: SpeechToText.Start falhou em iniciar.");
            isListening = false;
            if (listenButton != null) listenButton.interactable = true;
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
            Debug.LogError("ImageVoiceMatcher: ShowImage - Índice inválido: " + index + ". Tamanho da lista: " + wordList.Count);
            return;
        }

        displayImage.sprite = wordList[index].image;
        displayImage.color = Color.white;
        displayImage.preserveAspect = true;
        Debug.Log("ImageVoiceMatcher: ShowImage - Mostrando imagem para a palavra: '" + wordList[index].word + "' (Índice: " + index + ")");
    }

    void GoToNextImage()
    {
        Debug.Log("GoToNextImage: CHAMADO. currentIndex ANTES: " + currentIndex + " | Time.timeScale: " + Time.timeScale);
        currentIndex++;
        Debug.Log("GoToNextImage: currentIndex DEPOIS: " + currentIndex + " | Total na Lista: " + wordList.Count);

        if (currentIndex >= wordList.Count)
        {
            Debug.Log("GoToNextImage: FIM DA LISTA ALCANÇADO.");
            feedbackText.text = "🎉 Parabéns! Você completou todas as imagens! 🎉";
            if (displayImage != null) displayImage.enabled = false;
            ShowEndPhasePanel();
            if (listenButton != null) listenButton.interactable = false;
        }
        else
        {
            Debug.Log("GoToNextImage: MOSTRANDO PRÓXIMA IMAGEM (Índice: " + currentIndex + ").");
            ShowImage(currentIndex);
            feedbackText.text = initialMessage;
            if (listenButton != null) listenButton.interactable = true;
        }
    }

   private IEnumerator WaitAndAdvance()
{
    // --- APENAS PARA TESTE ---
    Time.timeScale = 1f;
    Debug.LogWarning("Time.timeScale FORÇADO PARA 1 DENTRO DA CORROTINA (APENAS TESTE!)");
    // --- FIM DO TESTE ---

    Debug.Log("CORROTINA WaitAndAdvance INICIADA. Time.timeScale: " + Time.timeScale);
    yield return new WaitForSeconds(delayAfterCorrect);
    Debug.Log("CORROTINA WaitAndAdvance: Delay Concluído. Chamando GoToNextImage().");
    GoToNextImage();
    isProcessing = false;
}

    // --- Implementação da Interface ISpeechToTextListener ---

    public void OnReadyForSpeech()
    {
        Debug.Log("ImageVoiceMatcher STT: OnReadyForSpeech - Pronto para ouvir.");
    }

    public void OnBeginningOfSpeech()
    {
        Debug.Log("ImageVoiceMatcher STT: OnBeginningOfSpeech - Usuário começou a falar.");
    }

    public void OnPartialResultReceived(string partialText)
    {
        // Debug.Log("ImageVoiceMatcher STT: OnPartialResultReceived - Resultado Parcial: " + partialText);
    }

    public void OnVoiceLevelChanged(float level)
    {
        // Para feedback visual do volume da voz
    }

    public void OnResultReceived(string recognizedText, int? errorCode)
{
    Debug.Log("ImageVoiceMatcher STT: OnResultReceived - Texto: '" + recognizedText + "', Código de Erro: " + (errorCode.HasValue ? errorCode.Value.ToString() : "Nenhum"));
    isListening = false;

    if (isProcessing)
    {
        Debug.LogWarning("ImageVoiceMatcher STT: Resultado recebido, mas já estava processando um anterior (isProcessing=true). Ignorando este.");
        if (listenButton != null && !SpeechToText.IsBusy()) listenButton.interactable = true;
        return;
    }

    if (errorCode.HasValue)
    {
       
        string friendlyErrorMessage = GetFriendlyErrorMessage(errorCode.Value);
        Debug.LogError($"ImageVoiceMatcher STT: Erro de reconhecimento - Código {errorCode.Value}. Mensagem: {friendlyErrorMessage}");
        feedbackText.text = friendlyErrorMessage;
    
        if (listenButton != null) listenButton.interactable = true;
        return;
    }
        if (string.IsNullOrEmpty(recognizedText))
        {
            Debug.LogWarning("ImageVoiceMatcher STT: OnResultReceived - Resultado vazio recebido (sem erro).");
            feedbackText.text = tryAgainMessage + "\n(Não ouvi nada)";
            if (listenButton != null) listenButton.interactable = true;
            return;
        }
        
        isProcessing = true; 

        string expectedWord = wordList[currentIndex].word.ToLower().Trim();
        string receivedWord = recognizedText.ToLower().Trim();

        Debug.Log($"ImageVoiceMatcher: OnResultReceived - Comparando... Esperado: '{expectedWord}', Recebido: '{receivedWord}'");

        if (receivedWord.Contains(expectedWord))
        {
            feedbackText.text = correctMessage;
            Debug.Log("ImageVoiceMatcher: ACERTOU!");
            AddScore(10);
            StartCoroutine(WaitAndAdvance()); 
        }
        else
        {
            feedbackText.text = tryAgainMessage + $"\n(Você disse: {receivedWord})";
            Debug.Log($"ImageVoiceMatcher: ERROU! Esperado: '{expectedWord}', Recebido: '{receivedWord}'");
            isProcessing = false; 
            if (listenButton != null) listenButton.interactable = true;
        }
    }
        private string GetFriendlyErrorMessage(int errorCode)
        {
    Debug.Log("GetFriendlyErrorMessage chamado com código: " + errorCode);
    switch (errorCode)
        {
        case 0: // Aparentemente, o plugin usa 0 para SpeechToText.Cancel()
            return "Escuta cancelada.";
        case 1: // SpeechRecognizer.ERROR_NETWORK_TIMEOUT
            return "Problema de rede. Verifique sua conexão e tente de novo.";
        case 2: // SpeechRecognizer.ERROR_NETWORK
            return "Erro de conexão. Tente novamente.";
        case 3: // SpeechRecognizer.ERROR_AUDIO
            return "Erro de áudio. Verifique seu microfone.";
        case 4: // SpeechRecognizer.ERROR_SERVER
            return "Erro no servidor de reconhecimento. Tente mais tarde.";
        case 5: // SpeechRecognizer.ERROR_CLIENT
            return "Ocorreu um problema. Tente de novo.";
        case 6: // SpeechRecognizer.ERROR_SPEECH_TIMEOUT (O plugin também mapeia o erro 7 para 6)
            return "Não ouvi nada ou não entendi. Fale mais alto e claro, por favor.";
        // case 7 (ERROR_NO_MATCH) é tratado como 6 pelo plugin, então a mensagem acima cobre isso.
        case 8: // SpeechRecognizer.ERROR_RECOGNIZER_BUSY
            return "O serviço de voz está ocupado. Tente em alguns segundos.";
        case 9: // SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS
            // O plugin tem SpeechToText.OpenGoogleAppSettings() para este caso
            return "O app Google precisa de permissão para usar o microfone. Verifique as configurações do app Google.";
        default:
            return $"Não entendi. Tente de novo. (Erro {errorCode})";
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
        Debug.Log("ClosePauseMenu: Time.timeScale = " + Time.timeScale);
    }

    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score.ToString("000");
        PauseMenu.SetActive(true);
        Time.timeScale = 0f;
        ScoreTransfer.Instance?.SetScore(score);
        Debug.Log("OpenPauseMenu: Time.timeScale = " + Time.timeScale);
    }

    public void ShowEndPhasePanel()
    {
        Debug.Log("ShowEndPhasePanel: CHAMADO. Score: " + score + " | Time.timeScale atual: " + Time.timeScale);
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString("000");

        if(endPhasePanel != null) endPhasePanel.SetActive(true);
        // Time.timeScale = 0f; // Você pode querer pausar aqui também
        ScoreTransfer.Instance?.SetScore(score);
        // Verifique se audioManager e end3 existem
        if(audioManager != null && audioManager.end3 != null) audioManager.PlaySFX(audioManager.end3);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;

        if (numberCounter != null) numberCounter.Value = score;
        ScoreTransfer.Instance?.SetScore(score);

        UpdateAllScoreDisplays();
        Debug.Log("AddScore: Pontuação atualizada para: " + score);
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