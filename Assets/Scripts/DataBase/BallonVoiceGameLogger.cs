using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class BalloonVoiceGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int loadCount = 0;
    private int balloonsPopped = 0;
    private int voiceAttempts = 0;
    private int voiceSuccesses = 0;
    private int voiceFailures = 0;

    private void Start()
    {
        if (FirebaseUserSession.Instance == null || string.IsNullOrEmpty(FirebaseUserSession.Instance.LoggedUser))
        {
            Debug.LogError("Usuário não logado no BalloonVoiceGameLogger!");
            return;
        }

        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/balloonVoiceGame/loadCount";

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

            Debug.Log("BalloonVoiceGame - LoadCount atualizado -> " + loadCount);
            UnityMainThreadDispatcher.Enqueue(() => StartNewSession());
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        balloonsPopped = 0;
        voiceAttempts = 0;
        voiceSuccesses = 0;
        voiceFailures = 0;

        Debug.Log("BalloonVoiceGame - Sessão iniciada: " + currentSessionId);
    }

    public float EndSession()
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogWarning("Nenhuma sessão ativa para finalizar.");
            return 0f;
        }

        float sessionEndTime = Time.time;
        float sessionDuration = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}";
        
        dbRef.Child(path).Child("endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
        dbRef.Child(path).Child("sessionDuration")
            .SetValueAsync(sessionDuration);

        UpdateStats();

        Debug.Log($"BalloonVoiceGame - Sessão finalizada: {currentSessionId}");
        Debug.Log($"Duração da sessão: {sessionDuration:F2} segundos");

        currentSessionId = null;
        return sessionDuration;
    }

    // MÉTODO ORIGINAL (mantido para compatibilidade)
    public void LogBalloonPop(string syllable, int syllableIndex)
    {
        LogBalloonPopWithPosition(syllable, syllableIndex, Vector2.zero);
    }

    // NOVO MÉTODO: Com posição
    public void LogBalloonPopWithPosition(string syllable, int syllableIndex, Vector2 position)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogWarning("Nenhuma sessão ativa. Ignorando registro de balão.");
            return;
        }

        balloonsPopped++;
        UpdateStats();

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/balloonPops";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var popData = new Dictionary<string, object>
        {
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "positionX", position.x },
            { "positionY", position.y },
            { "totalPopped", balloonsPopped },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(popData);

        Debug.Log($"BalloonVoiceGame - Balão estourado: {syllable} | Posição: ({position.x:F2}, {position.y:F2}) | Total: {balloonsPopped}");
    }

    public void LogVoiceAttempt(string expectedSyllable, string recognizedText, int syllableIndex, int attemptNumber, bool success)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogWarning("Nenhuma sessão ativa. Ignorando registro de voz.");
            return;
        }

        voiceAttempts++;
        if (success)
            voiceSuccesses++;
        else
            voiceFailures++;

        UpdateStats();

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/voiceAttempts";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var voiceData = new Dictionary<string, object>
        {
            { "expected", expectedSyllable },
            { "recognized", recognizedText },
            { "syllableIndex", syllableIndex },
            { "attemptNumber", attemptNumber },
            { "success", success },
            { "totalAttempts", voiceAttempts },
            { "successRate", CalculateSuccessRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(voiceData);

        Debug.Log($"BalloonVoiceGame - Voz: {expectedSyllable} -> {recognizedText} | Sucesso: {success}");
    }

    public void LogSyllableCompleted(string syllable, int syllableIndex, bool success, int balloonsUsed, int voiceAttemptsUsed)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogWarning("Nenhuma sessão ativa. Ignorando registro de sílaba.");
            return;
        }

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/syllables";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var syllableData = new Dictionary<string, object>
        {
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "success", success },
            { "balloonsUsed", balloonsUsed },
            { "voiceAttemptsUsed", voiceAttemptsUsed },
            { "successRate", CalculateSuccessRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(syllableData);

        Debug.Log($"BalloonVoiceGame - Sílabas {(success ? "completa" : "falhou")}: {syllable}");
    }

    public void LogGameCompleted(int totalSyllables, int totalBalloons, int totalVoiceAttempts, int successfulSyllables)
    {
        if (string.IsNullOrEmpty(currentSessionId))
        {
            Debug.LogWarning("Nenhuma sessão ativa. Ignorando registro de jogo completo.");
            return;
        }

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/gameCompletion";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var completionData = new Dictionary<string, object>
        {
            { "totalSyllables", totalSyllables },
            { "totalBalloons", totalBalloons },
            { "totalVoiceAttempts", totalVoiceAttempts },
            { "successfulSyllables", successfulSyllables },
            { "completionRate", (float)successfulSyllables / totalSyllables },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(completionData);

        Debug.Log($"BalloonVoiceGame - Jogo completo! Sílabas: {successfulSyllables}/{totalSyllables}");
    }

    private void UpdateStats()
    {
        float successRate = CalculateSuccessRate();

        var statsPath = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/stats";
        var statsData = new Dictionary<string, object>
        {
            { "balloonsPopped", balloonsPopped },
            { "voiceAttempts", voiceAttempts },
            { "voiceSuccesses", voiceSuccesses },
            { "voiceFailures", voiceFailures },
            { "voiceSuccessRate", successRate }
        };

        dbRef.Child(statsPath).UpdateChildrenAsync(statsData);
    }

    private float CalculateSuccessRate()
    {
        return voiceAttempts > 0 ? (float)voiceSuccesses / voiceAttempts : 0f;
    }

    public void RestartSession()
    {
        if (!string.IsNullOrEmpty(currentSessionId))
        {
            EndSession();
        }
        StartNewSession();
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