using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Precisamos implementar a interface do plugin para receber os callbacks
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
    public List<WordData> wordList; // Sua lista de palavras e imagens
    public string languageCode = "pt-BR"; // Código do idioma para o reconhecimento

    [Header("Referências da Interface (UI)")]
    public Image displayImage; // Onde a imagem será mostrada
    public TMP_Text feedbackText; // Onde o feedback (Acertou/Tente de novo) será mostrado
    public Button listenButton; // Botão para iniciar a escuta (opcional)

    [Header("Feedback")]
    public string correctMessage = "Muito bem! ✅";
    public string tryAgainMessage = "Quase lá! Tente de novo. ❌";
    public string listeningMessage = "Ouvindo... 🎤";
    public string initialMessage = "Pressione 'Ouvir' e diga o nome!";
    public float delayAfterCorrect = 1.5f; // Tempo de espera após acertar

    private int currentIndex = 0;
    private bool isListening = false;
    private bool isProcessing = false; // Flag para evitar processamento múltiplo



    

    [Header("==========Pause Menu==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;
    private AudioManager audioManager;





    void Start()
    {
        // Atualiza score na HUD
        score = ScoreTransfer.Instance?.Score ?? 0;
        if (numberCounter != null) numberCounter.Value = score;

        if (scoreHUD != null) scoreHUD.text = score.ToString("000");
        if (scorePause != null) scorePause.text = "Score: " + score.ToString("000");
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString("000");

        if (wordList == null || wordList.Count == 0)
        {
            feedbackText.text = "ERRO: Nenhuma palavra configurada!";
            Debug.LogError("A lista 'wordList' está vazia!");
            if (listenButton != null) listenButton.interactable = false;
            return;
        }

        if (displayImage == null || feedbackText == null)
        {
            Debug.LogError("ERRO: Referências de UI não configuradas no Inspector!");
            if (listenButton != null) listenButton.interactable = false;
            return;
        }

        // Inicializa o serviço de Speech-to-Text
        SpeechToText.Initialize(languageCode);
        Debug.Log("SpeechToText Inicializado com idioma: " + languageCode);

        // Configura o botão (se houver)
        if (listenButton != null)
        {
            listenButton.onClick.AddListener(OnListenButtonPressed);
        }

        // Mostra a primeira imagem
        ShowImage(currentIndex);
        feedbackText.text = initialMessage;

        // Verifica e pede permissão, se necessário
        CheckAndRequestPermission();
    }

    void CheckAndRequestPermission()
    {
        if (!SpeechToText.CheckPermission())
        {
            feedbackText.text = "Pedindo permissão...";
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    feedbackText.text = initialMessage;
                    Debug.Log("Permissão concedida!");
                }
                else
                {
                    feedbackText.text = "Permissão negada! Habilite nas configurações.";
                    Debug.LogError("Permissão de microfone negada!");
                    if (listenButton != null) listenButton.interactable = false;
                }
            });
        }
        else
        {
            Debug.Log("Permissão já concedida.");
        }
    }


    void OnListenButtonPressed()
    {
        // Não começa se já estiver ouvindo ou processando
        if (isListening || isProcessing || !SpeechToText.CheckPermission())
        {
            Debug.LogWarning("Não pode iniciar: isListening=" + isListening + ", isProcessing=" + isProcessing + ", Permissão=" + SpeechToText.CheckPermission());
            return;
        }

        feedbackText.text = listeningMessage;
        isListening = true;
        if (listenButton != null) listenButton.interactable = false; // Desativa o botão enquanto ouve

        // Inicia a escuta. O 'false' no segundo parâmetro (useFreeFormLanguageModel)
        // pode ser 'true' dependendo do que for melhor.
        // O 'false' no terceiro (preferOfflineRecognition) usa online por padrão.
        bool started = SpeechToText.Start(this, true, false);

        if (!started)
        {
            feedbackText.text = "Erro ao iniciar a escuta.";
            Debug.LogError("SpeechToText.Start falhou.");
            isListening = false;
            if (listenButton != null) listenButton.interactable = true;
        }
        else
        {
            Debug.Log("Escuta iniciada...");
        }
    }

    void ShowImage(int index)
    {
        if (index < 0 || index >= wordList.Count)
        {
            Debug.LogError("Índice inválido para ShowImage: " + index);
            return;
        }

        displayImage.sprite = wordList[index].image;
        displayImage.color = Color.white; // Garante visibilidade
        displayImage.preserveAspect = true; // Mantém a proporção
        Debug.Log("Mostrando imagem: " + wordList[index].word);

        // Opcional: Tocar o som da palavra
        // if(audioManager != null && wordList[index].wordAudio != null)
        // {
        //     audioManager.PlaySFX(wordList[index].wordAudio);
        // }
    }

    void GoToNextImage()
    {
        currentIndex++;
        if (currentIndex >= wordList.Count)
        {
            feedbackText.text = "🎉 Parabéns! Você completou todas as imagens! 🎉";
            displayImage.enabled = false;
            ShowEndPhasePanel();
            if (listenButton != null) listenButton.interactable = false;
            Debug.Log("Fim da lista de palavras.");
            // Aqui você pode chamar o painel de fim de fase, etc.
        }
        else
        {
            ShowImage(currentIndex);
            feedbackText.text = initialMessage;
            if (listenButton != null) listenButton.interactable = true; // Reativa o botão
        }
    }

    // --- Implementação da Interface ISpeechToTextListener ---

    public void OnReadyForSpeech()
    {
        Debug.Log("STT: Pronto para ouvir.");
        // Não precisamos fazer nada especial aqui por enquanto.
    }

    public void OnBeginningOfSpeech()
    {
        Debug.Log("STT: Começou a falar.");
        // Não precisamos fazer nada especial aqui por enquanto.
    }

    public void OnPartialResultReceived(string partialText)
    {
        // feedbackText.text = partialText + "..."; // Mostra o que está sendo dito
    }

    public void OnVoiceLevelChanged(float level)
    {
        // Poderia ser usado para animar um ícone de microfone, por exemplo.
    }

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        isListening = false; // A escuta terminou (com resultado ou erro)

        // Se já estamos processando, ou se não há texto e nem erro, saia.
        if (isProcessing || (string.IsNullOrEmpty(recognizedText) && !errorCode.HasValue))
        {
            if (listenButton != null) listenButton.interactable = true;
            return;
        }

        // Verifica se houve erro
        if (errorCode.HasValue)
        {
            Debug.LogError($"STT Erro: Código {errorCode.Value}");
            feedbackText.text = $"Erro {errorCode.Value}. Tente novamente.";
            if (listenButton != null) listenButton.interactable = true;
            return;
        }

        // Se chegamos aqui, temos um texto reconhecido.
        Debug.Log($"STT Resultado: '{recognizedText}'");
        isProcessing = true; // Marca que estamos processando este resultado

        string expected = wordList[currentIndex].word.ToLower().Trim();
        string received = recognizedText.ToLower().Trim();

        // Verificação - aqui você pode ajustar o quão "exata" a correspondência precisa ser.
        // Usar 'Contains' é mais flexível, mas pode gerar falsos positivos.
        // Usar '==' é mais exato.
        if (received.Contains(expected))
        {
            feedbackText.text = correctMessage;
            Debug.Log("ACERTOU!");
            AddScore(10);
            StartCoroutine(WaitAndAdvance());
        }
        else
        {
            feedbackText.text = tryAgainMessage + $"\n(Você disse: {recognizedText})";
            Debug.Log($"ERROU! Esperado: '{expected}', Recebido: '{received}'");
            isProcessing = false; // Libera para tentar de novo
            if (listenButton != null) listenButton.interactable = true;
        }
    }

    private IEnumerator WaitAndAdvance()
    {
        yield return new WaitForSeconds(delayAfterCorrect);
        GoToNextImage();
        isProcessing = false; // Libera o processamento para a próxima imagem
    }

    // --- Funções do Ciclo de Vida do Unity ---

    void OnDestroy()
    {
        // Garante que o botão pare de 'ouvir' se o objeto for destruído
        if (listenButton != null)
        {
            listenButton.onClick.RemoveListener(OnListenButtonPressed);
        }
        // Tenta cancelar qualquer escuta pendente
        if (SpeechToText.IsBusy())
        {
            SpeechToText.Cancel();
        }
    }










    #region Pause Menu and Score Management
    public void ClosePauseMenu()
    {
        PauseMenu.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        if (scorePause != null) scorePause.text = "Score: " + score.ToString();
        PauseMenu.SetActive(true);
        ScoreTransfer.Instance.SetScore(score);
    }

    public void ShowEndPhasePanel()
    {
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString();

        endPhasePanel.SetActive(true);
        ScoreTransfer.Instance.SetScore(score);
        audioManager.PlaySFX(audioManager.end3);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;

        numberCounter.Value = score;
        ScoreTransfer.Instance.SetScore(score);

        if (scorePause != null) scorePause.text = "Score: " + score.ToString("000");
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString("000");
        if (scoreHUD != null) scoreHUD.text = score.ToString("000");
    }
    #endregion
    
}