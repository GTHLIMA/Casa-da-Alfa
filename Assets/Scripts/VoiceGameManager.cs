using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class VoiceGameManager : MonoBehaviour, ISpeechToTextListener
{
    [Serializable]
    public class WordData
    {
        public string word;
        public Sprite image;
    }

    [Header("UI")]
    public Image wordImage;
    public Text feedbackText;

    [Header("Word List")]
    public List<WordData> words;

    private int currentIndex = 0;

    [Header("==========Pause Menu==========")]
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;
    public TMP_Text scoreHUD;
    public GameObject PauseMenu;
    [SerializeField] private GameObject endPhasePanel;
    [SerializeField] private NumberCounter numberCounter;

    


    void Start()
    {
        //Serve apenas para atualizar o visual do score
        score = ScoreTransfer.Instance.Score;
        numberCounter.Value = score;

        if (scoreHUD != null) scoreHUD.text = score.ToString("000");
        if (scorePause != null) scorePause.text = "Score: " + score.ToString("000");
        if (scoreEndPhase != null) scoreEndPhase.text = "Score: " + score.ToString("000");

        

        if (words.Count == 0)
        {
            feedbackText.text = "Nenhuma palavra configurada!";
            return;
        }

        SpeechToText.Initialize("pt-BR");

        // Verifica permissão de microfone
        if (!SpeechToText.CheckPermission())
        {
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    ShowCurrentWord();
                }
                else
                {
                    feedbackText.text = "Permissão negada!";
                }
            });
        }
        else
        {
            ShowCurrentWord();
        }
    }

    public void StartListening()
    {
        feedbackText.text = "Fale agora...";

        bool started = SpeechToText.Start(this, true, false);
        if (!started)
        {
            feedbackText.text = "Não foi possível iniciar a escuta.";
        }
    }

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        if (errorCode.HasValue)
        {
            feedbackText.text = $"Erro: código {errorCode.Value}";
            return;
        }

        string expected = words[currentIndex].word.ToLower();
        string received = recognizedText.ToLower();

        feedbackText.text = $"Você disse: {recognizedText}";

        if (received.Contains(expected))
        {
            feedbackText.text += "\n✅ Acertou!";
            Invoke(nameof(NextWord), 2f);
            AddScore(10); // Adiciona 10 pontos por palavra correta
        }
        else
        {
            feedbackText.text += "\n❌ Tente novamente.";
        }
    }

    public void OnReadyForSpeech()
    {
        // Pode mostrar algum feedback visual
    }

    public void OnBeginningOfSpeech()
    {
        // Pode vibrar ou animar algo
    }

    public void OnPartialResultReceived(string partialText)
    {
        // Feedback em tempo real se desejar
    }

    public void OnVoiceLevelChanged(float level)
    {
        // Pode mostrar visual do nível de voz
    }

    private void ShowCurrentWord()
    {
        wordImage.sprite = words[currentIndex].image;
        feedbackText.text = "Diga o nome do que vê!";
    }

    private void NextWord()
    {
        currentIndex++;
        if (currentIndex >= words.Count)
        {
            feedbackText.text = "🎉 Parabéns! Você terminou!";
        }
        else
        {
            ShowCurrentWord();
        }
    }


//Hud do score e pause menu
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
        if (scoreEndPhase != null)
            scoreEndPhase.text = "Score: " + score.ToString();

        endPhasePanel.SetActive(true);
        ScoreTransfer.Instance.SetScore(score);
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




}
