using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

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

    void Start()
    {
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
}
