using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class SyllableGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int loadCount = 0;
    private int correctClicks = 0;
    private int wrongClicks = 0;

    private void Start()
    {
        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/syllableGame/loadCount";

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

            Debug.Log("SyllableGame - LoadCount atualizado -> " + loadCount);
            UnityMainThreadDispatcher.Enqueue(() => StartNewSession());
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/syllableGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        correctClicks = 0;
        wrongClicks = 0;

        Debug.Log("SyllableGame - Sessao iniciada: " + currentSessionId + " | Tempo: " + sessionStartTime);
    }

    public float EndSession()
    {
        float sessionEndTime = Time.time;
        float sessionDuration = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/syllableGame/sessions/{currentSessionId}";
        
        dbRef.Child(path).Child("endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
        dbRef.Child(path).Child("sessionDuration")
            .SetValueAsync(sessionDuration);

        Debug.Log($"SyllableGame - Sessao finalizada: {currentSessionId}");
        Debug.Log($"Duracao da sessao: {sessionDuration:F2} segundos");
        Debug.Log($"Inicio: {sessionStartTime:F2} | Fim: {sessionEndTime:F2}");

        return sessionDuration;
    }

    public void LogCorrectClick(string word, int puzzleIndex, int clickIndex)
    {
        correctClicks++;
        UpdateStats();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string path = $"users/{username}/syllableGame/sessions/{currentSessionId}/clicks";
        
        var clickData = new Dictionary<string, object>
        {
            { "type", "correct" },
            { "word", word },
            { "puzzleIndex", puzzleIndex },
            { "clickIndex", clickIndex },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(clickData);

        Debug.Log("SyllableGame - Acerto -> " + word + $" | Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogWrongClick(string expected, string received, int puzzleIndex, int clickIndex)
    {
        wrongClicks++;
        UpdateStats();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string path = $"users/{username}/syllableGame/sessions/{currentSessionId}/clicks";
        
        var clickData = new Dictionary<string, object>
        {
            { "type", "wrong" },
            { "expected", expected },
            { "received", received },
            { "puzzleIndex", puzzleIndex },
            { "clickIndex", clickIndex },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(clickData);

        Debug.Log($"SyllableGame - Erro -> esperado: {expected}, clicado: {received} | Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogPuzzleCompleted(string word, int puzzleIndex, bool success)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string path = $"users/{username}/syllableGame/sessions/{currentSessionId}/puzzles";
        
        var puzzleData = new Dictionary<string, object>
        {
            { "word", word },
            { "puzzleIndex", puzzleIndex },
            { "success", success },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };
        
        dbRef.Child(path).Push().SetValueAsync(puzzleData);

        Debug.Log($"SyllableGame - Puzzle {(success ? "completo" : "falhou")} -> {word}");
    }

    private void UpdateStats()
    {
        float accuracyRate = CalculateAccuracyRate();

        var statsPath = $"users/{username}/syllableGame/sessions/{currentSessionId}/stats";
        var statsData = new Dictionary<string, object>
        {
            { "correctClicks", correctClicks },
            { "wrongClicks", wrongClicks },
            { "accuracyRate", accuracyRate }
        };
        
        dbRef.Child(statsPath).UpdateChildrenAsync(statsData);
    }

    private float CalculateAccuracyRate()
    {
        int total = correctClicks + wrongClicks;
        return total > 0 ? (float)correctClicks / total : 0f;
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