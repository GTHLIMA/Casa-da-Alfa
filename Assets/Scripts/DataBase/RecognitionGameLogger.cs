using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class RecognitionGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int totalQuestions = 0;
    private int correctAnswers = 0;
    private int wrongAnswers = 0;
    private int currentQuestionIndex = 0;

    private void Start()
    {
        Debug.Log("RECOGNITION GAME LOGGER INICIADO");
        
        // Busca o usuário logado
        if (FirebaseUserSession.Instance != null)
        {
            username = FirebaseUserSession.Instance.LoggedUser;
            Debug.Log("Usuario encontrado: " + username);
        }
        else
        {
            username = "UsuarioDesconhecido";
            Debug.LogWarning("FirebaseUserSession.Instance é null! Usando usuário padrão.");
        }
        
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("DatabaseReference inicializado");

        StartNewSession();
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/recognitionGame/sessions/{currentSessionId}";
        
        var sessionData = new Dictionary<string, object>
        {
            { "iniciadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "tipoJogo", "reconhecimento_silabas" },
            { "usuario", username }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(sessionData).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Sessao Recognition Game criada no Firebase: " + currentSessionId);
            }
            else
            {
                Debug.LogError("Erro ao criar sessao: " + task.Exception);
            }
        });

        Debug.Log("Sessao Recognition Game iniciada: " + currentSessionId);
    }

    public void LogQuestionStart(string syllable, string correctImage, int questionNumber)
    {
        currentQuestionIndex = questionNumber;
        totalQuestions++;

        string path = $"users/{username}/recognitionGame/sessions/{currentSessionId}/perguntas";
        
        var questionData = new Dictionary<string, object>
        {
            { "numeroPergunta", questionNumber },
            { "silaba", syllable },
            { "imagemCorreta", correctImage },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "tipo", "inicio_pergunta" }
        };
        
        dbRef.Child(path).Push().SetValueAsync(questionData);

        Debug.Log("Pergunta " + questionNumber + " iniciada - Silaba: " + syllable + ", Imagem Correta: " + correctImage);
    }

    public void LogAnswer(bool wasCorrect, string syllable, string selectedImage, string correctImage, float responseTime)
    {
        if (wasCorrect)
        {
            correctAnswers++;
            Debug.Log("RESPOSTA CORRETA: Silaba " + syllable + " -> Imagem " + selectedImage);
        }
        else
        {
            wrongAnswers++;
            Debug.Log("RESPOSTA ERRADA: Silaba " + syllable + " -> Selecionou " + selectedImage + ", Esperado " + correctImage);
        }

        string path = $"users/{username}/recognitionGame/sessions/{currentSessionId}/respostas";
        
        var answerData = new Dictionary<string, object>
        {
            { "correta", wasCorrect },
            { "silaba", syllable },
            { "imagemSelecionada", selectedImage },
            { "imagemCorreta", correctImage },
            { "tempoResposta", responseTime },
            { "numeroPergunta", currentQuestionIndex },
            { "totalCorretas", correctAnswers },
            { "totalErradas", wrongAnswers },
            { "acuracia", CalcularTaxaAcuracia() },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        dbRef.Child(path).Push().SetValueAsync(answerData);
    }

    // public void LogSyllablePlay(string syllable, int questionNumber)
    // {
    //     string path = $"users/{username}/recognitionGame/sessions/{currentSessionId}/eventos";
        
    //     var eventData = new Dictionary<string, object>
    //     {
    //         { "tipo", "audio_silaba_reproduzido" },
    //         { "silaba", syllable },
    //         { "numeroPergunta", questionNumber },
    //         { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
    //     };
        
    //     dbRef.Child(path).Push().SetValueAsync(eventData);

    //     Debug.Log("Audio da silaba reproduzido: " + syllable + " (Pergunta " + questionNumber + ")");
    // }

    public void LogSessionEnd(int totalQuestionsCompleted)
    {
        Debug.Log("RECOGNITION GAME SESSION END");
        
        float sessionEndTime = Time.time;
        float totalPlayTime = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/recognitionGame/sessions/{currentSessionId}";
        
        var endData = new Dictionary<string, object>
        {
            { "finalizadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "tempoTotalJogo", totalPlayTime },
            { "totalPerguntas", totalQuestionsCompleted },
            { "respostasCorretas", correctAnswers },
            { "respostasErradas", wrongAnswers },
            { "acuraciaFinal", CalcularTaxaAcuracia() },
            { "status", "completado" }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(endData).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Sessao Recognition Game finalizada!");
                Debug.Log("Estatisticas Finais:");
                Debug.Log("   Tempo Total: " + totalPlayTime.ToString("F1") + "s");
                Debug.Log("   Perguntas: " + totalQuestionsCompleted);
                Debug.Log("   Respostas Corretas: " + correctAnswers);
                Debug.Log("   Respostas Erradas: " + wrongAnswers);
                Debug.Log("   Acuracia: " + CalcularTaxaAcuracia() + "%");
            }
        });
    }

    private int CalcularTaxaAcuracia()
    {
        int totalAnswers = correctAnswers + wrongAnswers;
        if (totalAnswers > 0)
        {
            float accuracy = (float)correctAnswers / totalAnswers * 100f;
            return Mathf.RoundToInt(accuracy);
        }
        return 0;
    }

    // Método para debug
    public void PrintCurrentState()
    {
        Debug.Log("ESTADO ATUAL RECOGNITION GAME LOGGER");
        Debug.Log("SessionID: " + currentSessionId);
        Debug.Log("Pergunta Atual: " + currentQuestionIndex);
        Debug.Log("Total Perguntas: " + totalQuestions);
        Debug.Log("Respostas Corretas: " + correctAnswers);
        Debug.Log("Respostas Erradas: " + wrongAnswers);
        Debug.Log("Acuracia: " + CalcularTaxaAcuracia() + "%");
        Debug.Log("Tempo de Sessao: " + (Time.time - sessionStartTime).ToString("F1") + "s");
        Debug.Log("=============================");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit - Finalizando sessao Recognition Game...");
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy - Finalizando sessao Recognition Game...");
    }
}