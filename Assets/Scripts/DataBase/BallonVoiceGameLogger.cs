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
    
    // Dados da sessão atual
    private int currentSyllableIndex = 0;
    private string currentSyllable = "";
    private int balloonsPoppedThisRound = 0;
    private int voiceAttemptsThisRound = 0;
    private int voiceFailuresThisRound = 0;
    private bool voiceSuccessThisRound = false;

    // Estatísticas acumuladas
    private int totalBalloonsPopped = 0;
    private int totalVoiceAttempts = 0;
    private int totalVoiceSuccesses = 0;
    private int totalVoiceFailures = 0;
    private int totalSyllablesCompleted = 0;

    private bool firebaseInitialized = false;

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (FirebaseUserSession.Instance == null)
        {
            Debug.LogWarning("FirebaseUserSession não encontrado - tentando novamente...");
            Invoke(nameof(InitializeFirebase), 1f);
            return;
        }

        if (string.IsNullOrEmpty(FirebaseUserSession.Instance.LoggedUser))
        {
            Debug.LogWarning("Usuário não logado - Logger desativado");
            return;
        }

        username = FirebaseUserSession.Instance.LoggedUser;
        
        try
        {
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            firebaseInitialized = true;
            Debug.Log($"✅ Logger Firebase inicializado para usuário: {username}");
            
            LoadAndIncrementLoadCount();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Erro ao inicializar Firebase: {e.Message}");
            firebaseInitialized = false;
        }
    }

    private void LoadAndIncrementLoadCount()
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("Firebase não inicializado - pulando LoadCount");
            StartNewSession();
            return;
        }

        string path = $"users/{username}/balloonVoiceGame/loadCount";

        dbRef.Child(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result != null && task.Result.Exists)
            {
                int.TryParse(task.Result.Value.ToString(), out loadCount);
            }
            else
            {
                loadCount = 0;
            }

            loadCount++;
            dbRef.Child(path).SetValueAsync(loadCount);

            Debug.Log($"🎮 BalloonVoiceGame - LoadCount: {loadCount}");
            StartNewSession();
        });
    }

    private void StartNewSession()
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("Firebase não inicializado - sessão não criada");
            return;
        }

        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        // Reset dados da sessão
        currentSyllableIndex = 0;
        currentSyllable = "";
        balloonsPoppedThisRound = 0;
        voiceAttemptsThisRound = 0;
        voiceFailuresThisRound = 0;
        voiceSuccessThisRound = false;
        
        totalBalloonsPopped = 0;
        totalVoiceAttempts = 0;
        totalVoiceSuccesses = 0;
        totalVoiceFailures = 0;
        totalSyllablesCompleted = 0;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}";
        
        var sessionData = new Dictionary<string, object>
        {
            { "startedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "loadCount", loadCount },
            { "device", SystemInfo.deviceModel },
            { "platform", Application.platform.ToString() },
            { "gameVersion", Application.version }
        };

        dbRef.Child(path).UpdateChildrenAsync(sessionData).ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"✅ Sessão criada no Firebase: {currentSessionId}");
                
                // Log do primeiro acesso ao jogo
                LogGameEvent("game_started", new Dictionary<string, object>
                {
                    { "sessionId", currentSessionId },
                    { "timestamp", DateTime.Now.ToString("HH:mm:ss") }
                });
            }
            else
            {
                Debug.LogError($"❌ Falha ao criar sessão: {task.Exception}");
            }
        });
    }

    public void LogRoundStart(string syllable, int syllableIndex)
    {
        if (!firebaseInitialized || string.IsNullOrEmpty(currentSessionId)) 
        {
            Debug.LogWarning("Logger não pronto - pulando LogRoundStart");
            return;
        }

        currentSyllable = syllable;
        currentSyllableIndex = syllableIndex;
        balloonsPoppedThisRound = 0;
        voiceAttemptsThisRound = 0;
        voiceFailuresThisRound = 0;
        voiceSuccessThisRound = false;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/rounds";
        
        var roundData = new Dictionary<string, object>
        {
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "startedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "balloonsRequired", GetBalloonsRequired() }
        };

        dbRef.Child(path).Push().SetValueAsync(roundData);
        Debug.Log($"🎯 Round iniciado: {syllable} (índice {syllableIndex})");
    }

    public void LogBalloonPop(Vector2 screenPosition)
    {
        if (!firebaseInitialized || string.IsNullOrEmpty(currentSessionId)) 
        {
            Debug.LogWarning("Logger não pronto - pulando LogBalloonPop");
            return;
        }

        balloonsPoppedThisRound++;
        totalBalloonsPopped++;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/balloonPops";
        
        var popData = new Dictionary<string, object>
        {
            { "syllable", currentSyllable },
            { "syllableIndex", currentSyllableIndex },
            { "positionX", screenPosition.x / Screen.width },
            { "positionY", screenPosition.y / Screen.height },
            { "roundBalloons", balloonsPoppedThisRound },
            { "totalBalloons", totalBalloonsPopped },
            { "timestamp", DateTime.Now.ToString("HH:mm:ss") }
        };

        dbRef.Child(path).Push().SetValueAsync(popData);
        Debug.Log($"🎈 Balão #{balloonsPoppedThisRound} estourado - Sílaba: {currentSyllable}");
    }

    public void LogVoiceAttempt(string recognizedText, bool success)
    {
        if (!firebaseInitialized || string.IsNullOrEmpty(currentSessionId)) 
        {
            Debug.LogWarning("Logger não pronto - pulando LogVoiceAttempt");
            return;
        }

        voiceAttemptsThisRound++;
        totalVoiceAttempts++;

        if (success)
        {
            totalVoiceSuccesses++;
            voiceSuccessThisRound = true;
        }
        else
        {
            voiceFailuresThisRound++;
            totalVoiceFailures++;
        }

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/voiceAttempts";
        
        var voiceData = new Dictionary<string, object>
        {
            { "syllable", currentSyllable },
            { "syllableIndex", currentSyllableIndex },
            { "expected", currentSyllable },
            { "recognized", recognizedText },
            { "success", success },
            { "attemptNumber", voiceAttemptsThisRound },
            { "roundFailures", voiceFailuresThisRound },
            { "totalAttempts", totalVoiceAttempts },
            { "totalSuccessRate", CalculateTotalSuccessRate() },
            { "timestamp", DateTime.Now.ToString("HH:mm:ss") }
        };

        dbRef.Child(path).Push().SetValueAsync(voiceData);
        Debug.Log($"🎤 Voz: '{recognizedText}' -> {(success ? "✅ CORRETO" : "❌ INCORRETO")}");
    }

    public void LogRoundComplete(bool success)
    {
        if (!firebaseInitialized || string.IsNullOrEmpty(currentSessionId)) 
        {
            Debug.LogWarning("Logger não pronto - pulando LogRoundComplete");
            return;
        }

        if (success)
        {
            totalSyllablesCompleted++;
        }

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/roundsCompletion";
        
        var completionData = new Dictionary<string, object>
        {
            { "syllable", currentSyllable },
            { "syllableIndex", currentSyllableIndex },
            { "success", success },
            { "balloonsUsed", balloonsPoppedThisRound },
            { "voiceAttempts", voiceAttemptsThisRound },
            { "voiceFailures", voiceFailuresThisRound },
            { "voiceSuccess", voiceSuccessThisRound },
            { "completionTime", Time.time - sessionStartTime },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        dbRef.Child(path).Push().SetValueAsync(completionData);

        UpdateSessionStats();

        Debug.Log($"📊 Round {(success ? "✅ COMPLETO" : "❌ FALHOU")}: {currentSyllable}");
    }

    public void LogGameCompleted(int totalSyllables)
    {
        if (!firebaseInitialized || string.IsNullOrEmpty(currentSessionId)) 
        {
            Debug.LogWarning("Logger não pronto - pulando LogGameCompleted");
            return;
        }

        float sessionDuration = Time.time - sessionStartTime;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}";
        
        var gameData = new Dictionary<string, object>
        {
            { "endedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "sessionDuration", sessionDuration },
            { "totalSyllables", totalSyllables },
            { "completedSyllables", totalSyllablesCompleted },
            { "completionRate", (float)totalSyllablesCompleted / totalSyllables },
            { "totalBalloonsPopped", totalBalloonsPopped },
            { "totalVoiceAttempts", totalVoiceAttempts },
            { "totalVoiceSuccesses", totalVoiceSuccesses },
            { "totalVoiceFailures", totalVoiceFailures },
            { "finalSuccessRate", CalculateTotalSuccessRate() }
        };

        dbRef.Child(path).UpdateChildrenAsync(gameData);

        UpdateUserStats(totalSyllables);

        // Log de evento de fim de jogo
        LogGameEvent("game_completed", new Dictionary<string, object>
        {
            { "totalSyllables", totalSyllables },
            { "completedSyllables", totalSyllablesCompleted },
            { "sessionDuration", sessionDuration }
        });

        Debug.Log($"🏆 Jogo completo! {totalSyllablesCompleted}/{totalSyllables} sílabas - Duração: {sessionDuration:F2}s");
    }

    private void UpdateSessionStats()
    {
        if (!firebaseInitialized) return;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/stats";
        
        var statsData = new Dictionary<string, object>
        {
            { "currentSyllableIndex", currentSyllableIndex },
            { "totalBalloonsPopped", totalBalloonsPopped },
            { "totalVoiceAttempts", totalVoiceAttempts },
            { "totalVoiceSuccesses", totalVoiceSuccesses },
            { "totalVoiceFailures", totalVoiceFailures },
            { "totalSyllablesCompleted", totalSyllablesCompleted },
            { "successRate", CalculateTotalSuccessRate() },
            { "lastUpdate", DateTime.Now.ToString("HH:mm:ss") }
        };

        dbRef.Child(path).UpdateChildrenAsync(statsData);
    }

    private void UpdateUserStats(int totalSyllables)
    {
        if (!firebaseInitialized) return;

        string path = $"users/{username}/balloonVoiceGame/stats";
        
        dbRef.Child(path).GetValueAsync().ContinueWith(task =>
        {
            int totalGames = 0;
            int totalSyllablesCompleted = 0;
            int totalBalloons = 0;
            int totalVoiceAttempts = 0;

            if (task.IsCompleted && task.Result != null && task.Result.Exists)
            {
                var data = task.Result.Value as Dictionary<string, object>;
                if (data != null)
                {
                    if (data.ContainsKey("totalGames")) int.TryParse(data["totalGames"].ToString(), out totalGames);
                    if (data.ContainsKey("totalSyllablesCompleted")) int.TryParse(data["totalSyllablesCompleted"].ToString(), out totalSyllablesCompleted);
                    if (data.ContainsKey("totalBalloons")) int.TryParse(data["totalBalloons"].ToString(), out totalBalloons);
                    if (data.ContainsKey("totalVoiceAttempts")) int.TryParse(data["totalVoiceAttempts"].ToString(), out totalVoiceAttempts);
                }
            }

            totalGames++;
            totalSyllablesCompleted += this.totalSyllablesCompleted;
            totalBalloons += totalBalloonsPopped;
            totalVoiceAttempts += totalVoiceAttempts;

            var userStats = new Dictionary<string, object>
            {
                { "totalGames", totalGames },
                { "totalSyllablesCompleted", totalSyllablesCompleted },
                { "totalBalloons", totalBalloons },
                { "totalVoiceAttempts", totalVoiceAttempts },
                { "lastPlayed", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "averageSuccessRate", CalculateTotalSuccessRate() }
            };

            dbRef.Child(path).UpdateChildrenAsync(userStats);
        });
    }

    private void LogGameEvent(string eventType, Dictionary<string, object> eventData)
    {
        if (!firebaseInitialized) return;

        string path = $"users/{username}/balloonVoiceGame/sessions/{currentSessionId}/events";
        
        var eventLog = new Dictionary<string, object>(eventData)
        {
            { "eventType", eventType },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        dbRef.Child(path).Push().SetValueAsync(eventLog);
    }

    private float CalculateTotalSuccessRate()
    {
        return totalVoiceAttempts > 0 ? (float)totalVoiceSuccesses / totalVoiceAttempts : 0f;
    }

    private int GetBalloonsRequired()
    {
        var mm = MainGameManager.Instance;
        return mm != null ? mm.popsToComplete : 5;
    }

    // Métodos para serem chamados do MainGameManager
    public void OnSyllableStarted(string syllable, int index)
    {
        LogRoundStart(syllable, index);
    }

    public void OnBalloonPopped(Vector2 position)
    {
        LogBalloonPop(position);
    }

    public void OnVoiceAttempt(string recognized, bool success)
    {
        LogVoiceAttempt(recognized, success);
    }

    public void OnSyllableCompleted(bool success)
    {
        LogRoundComplete(success);
    }

    public void OnGameCompleted(int totalSyllables)
    {
        LogGameCompleted(totalSyllables);
    }

    public bool IsLoggerReady()
    {
        return firebaseInitialized && !string.IsNullOrEmpty(currentSessionId);
    }

    private void OnApplicationQuit()
    {
        if (IsLoggerReady())
        {
            LogGameCompleted(currentSyllableIndex + 1);
        }
    }

    private void OnDestroy()
    {
        if (IsLoggerReady())
        {
            LogGameCompleted(currentSyllableIndex + 1);
        }
    }
}