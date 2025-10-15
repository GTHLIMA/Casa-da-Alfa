using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class VoiceGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int loadCount = 0;
    private int correctCount = 0;
    private int errorCount = 0;
    private int hintCount = 0;

    private void Start()
    {
        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/voiceGame/loadCount";

        dbRef.Child(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int.TryParse(task.Result.Value.ToString(), out loadCount);
            }
            else
            {
                loadCount = 0;
            }

            loadCount++;
            dbRef.Child(path).SetValueAsync(loadCount);

            Debug.Log("VoiceGame - LoadCount atualizado -> " + loadCount);
            UnityMainThreadDispatcher.Enqueue(() => StartNewSession());
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        correctCount = 0;
        errorCount = 0;
        hintCount = 0;

        Debug.Log("VoiceGame - Sessao iniciada: " + currentSessionId + " | Tempo: " + sessionStartTime);
    }

    public float EndSession()
    {
        float sessionEndTime = Time.time;
        float sessionDuration = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}";
        
        dbRef.Child(path).Child("endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
        dbRef.Child(path).Child("sessionDuration")
            .SetValueAsync(sessionDuration);

        Debug.Log($"VoiceGame - Sessao finalizada: {currentSessionId}");
        Debug.Log($"Duracao da sessao: {sessionDuration:F2} segundos");
        Debug.Log($"Inicio: {sessionStartTime:F2} | Fim: {sessionEndTime:F2}");

        return sessionDuration;
    }

    public void LogImageProgress(string word, int index)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}/progress";
        
        var progressData = new Dictionary<string, object>
        {
            { "word", word },
            { "index", index },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(progressData);

        Debug.Log($"Progresso imagem {index} -> {word}, Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogCorrect(string word)
    {
        correctCount++;
        UpdateStats();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}/events";
        
        var eventData = new Dictionary<string, object>
        {
            { "type", "correct" },
            { "word", word },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(eventData);

        Debug.Log("Acerto -> " + word + $" | Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogError(string expected, string received)
    {
        errorCount++;
        UpdateStats();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}/events";
        
        var eventData = new Dictionary<string, object>
        {
            { "type", "error" },
            { "expected", expected },
            { "received", received },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(eventData);

        Debug.Log($"Erro -> esperado: {expected}, reconhecido: {received} | Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogHint(string word, int mistakeCount)
    {
        hintCount++;
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string statsPath = $"users/{username}/voiceGame/sessions/{currentSessionId}/stats";
        dbRef.Child(statsPath).Child("hints").SetValueAsync(hintCount);

        string eventsPath = $"users/{username}/voiceGame/sessions/{currentSessionId}/events";
        
        var eventData = new Dictionary<string, object>
        {
            { "type", "hint" },
            { "word", word },
            { "mistakeCount", mistakeCount },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(eventsPath).Push().SetValueAsync(eventData);

        Debug.Log("Ajuda -> " + word);
    }

    private void UpdateStats()
    {
        float accuracyRate = CalculateAccuracyRate();

        var statsPath = $"users/{username}/voiceGame/sessions/{currentSessionId}/stats";
        var statsData = new Dictionary<string, object>
        {
            { "corrects", correctCount },
            { "errors", errorCount },
            { "hints", hintCount },
            { "accuracyRate", accuracyRate }
        };
        
        dbRef.Child(statsPath).UpdateChildrenAsync(statsData);
    }

    private float CalculateAccuracyRate()
    {
        int total = correctCount + errorCount;
        return total > 0 ? (float)correctCount / total : 0f;
    }

    private void OnApplicationQuit()
    {
        EndSession();
    }

    private void OnDestroy()
    {
        EndSession();
    }
}