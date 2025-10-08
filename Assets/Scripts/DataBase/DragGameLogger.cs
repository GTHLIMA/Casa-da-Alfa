using UnityEngine;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class DragGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string parentSessionId;
    private string currentLevelId;
    
    private int loadCount = 0;
    private int totalLevelsCompleted = 0;
    private float parentSessionStartTime;
    private float levelStartTime;
    
    // Dados acumulados da sessão pai
    private int totalCorrectMatches = 0;
    private int totalErrors = 0;
    
    // Dados do nível atual
    private int levelCorrectMatches = 0;
    private int levelErrors = 0;

    // Tempo de arrasto
    private float dragStartTime;
    private bool isDragging;

    // Configuração dos índices das cenas
    private const int FIRST_LEVEL_INDEX = 3;
    private const int LAST_LEVEL_INDEX = 12;
    private const int TOTAL_LEVELS = 10;

    private int currentLevelIndex = -1;

    // Eventos
    public System.Action<string> OnParentSessionStarted;
    public System.Action<string, string, int> OnLevelStarted;
    public System.Action<string, int, int> OnLevelCompleted;

    private void Start()
    {
        if (FirebaseUserSession.Instance == null || string.IsNullOrEmpty(FirebaseUserSession.Instance.LoggedUser))
        {
            Debug.LogError("Usuario não logado no DragGameLogger!");
            return;
        }

        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        StartCoroutine(InitializeDragGameLogger());
    }

    private IEnumerator InitializeDragGameLogger()
    {
        yield return StartCoroutine(LoadAndIncrementLoadCount());
        StartParentSession();
    }

    private IEnumerator LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/dragGame/loadCount";

        var loadTask = dbRef.Child(path).GetValueAsync();
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.IsCompleted && !loadTask.IsFaulted && loadTask.Result != null && loadTask.Result.Exists)
        {
            int.TryParse(loadTask.Result.Value.ToString(), out loadCount);
        }
        else
        {
            loadCount = 0;
        }

        loadCount++;
        var saveTask = dbRef.Child(path).SetValueAsync(loadCount);
        yield return new WaitUntil(() => saveTask.IsCompleted);

        Debug.Log($"DragGame - LoadCount: {loadCount}");
    }

    // ===== SESSÃO PAI =====
    private void StartParentSession()
    {
        parentSessionId = Guid.NewGuid().ToString();
        parentSessionStartTime = Time.time;
        
        totalCorrectMatches = 0;
        totalErrors = 0;
        currentLevelIndex = FIRST_LEVEL_INDEX;

        var parentSessionData = new Dictionary<string, object>
        {
            { "startedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "loadCount", loadCount }
        };

        dbRef.Child($"users/{username}/dragGame/parentSessions/{parentSessionId}")
            .UpdateChildrenAsync(parentSessionData);

        Debug.Log($"Sessao Pai iniciada: {parentSessionId}");
        OnParentSessionStarted?.Invoke(parentSessionId);
        
        StartCurrentLevel();
    }

    private void StartCurrentLevel()
    {
        if (currentLevelIndex < FIRST_LEVEL_INDEX || currentLevelIndex > LAST_LEVEL_INDEX)
        {
            Debug.LogError($"Indice invalido: {currentLevelIndex}");
            return;
        }

        currentLevelId = Guid.NewGuid().ToString();
        levelStartTime = Time.time;
        
        levelCorrectMatches = 0;
        levelErrors = 0;

        string levelName = SceneManager.GetActiveScene().name;
        
        Debug.Log($"Iniciando nivel: {levelName}");

        var levelData = new Dictionary<string, object>
        {
            { "startedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "levelName", levelName }
        };

        string path = $"users/{username}/dragGame/parentSessions/{parentSessionId}/levels/{currentLevelId}";
        dbRef.Child(path).UpdateChildrenAsync(levelData);

        OnLevelStarted?.Invoke(parentSessionId, levelName, currentLevelIndex);
    }

    // ===== EVENTOS DE ARRASTO =====
    
    public void LogDragStart(string imageType, Vector2 position)
    {
        if (isDragging) return;
        
        dragStartTime = Time.time;
        isDragging = true;
        
        Debug.Log($"Drag iniciado: {imageType} em {position}");
    }

    public void LogDragEnd(string imageType, Vector2 startPosition, Vector2 endPosition, bool wasDropped)
    {
        if (!isDragging) return;
        
        float dragTime = Time.time - dragStartTime;
        float dragDistance = Vector2.Distance(startPosition, endPosition);
        isDragging = false;
        
        Debug.Log($"Drag finalizado: {imageType} - Tempo: {dragTime:F2}s, Distancia: {dragDistance:F2}, Drop: {wasDropped}");
    }

    // ===== EVENTOS PRINCIPAIS =====
    
    public void LogCorrectMatch(string imageType, string targetType)
    {
        levelCorrectMatches++;
        totalCorrectMatches++;

        Debug.Log($"Acerto: {imageType} -> {targetType}");

        UpdateLevelCounters();
        UpdateParentSessionCounters();
    }

    public void LogError(string expectedType, string receivedType, string errorType)
    {
        levelErrors++;
        totalErrors++;

        Debug.Log($"Erro: Esperado {expectedType}, Recebido {receivedType}, Tipo: {errorType}");

        UpdateLevelCounters();
        UpdateParentSessionCounters();
    }

    // ===== CONTROLE DE NÍVEL =====
    
    public void CompleteCurrentLevel(bool success, int matchesCompleted = 0, string reason = "completed")
    {
        if (matchesCompleted > 0) levelCorrectMatches = matchesCompleted;

        CompleteLevel(success, reason);

        if (success)
        {
            totalLevelsCompleted++;
            
            CompleteParentSession();
            
            Debug.Log($"Nivel completado com sucesso! - Acertos: {levelCorrectMatches}, Erros: {levelErrors}");
        }
    }

    public void RestartCurrentLevel(string reason = "restart")
    {
        CompleteLevel(false, reason);
    }

    public void FailCurrentLevel(string reason = "failed")
    {
        CompleteLevel(false, reason);
    }

    // ===== MÉTODOS INTERNOS =====
    private void CompleteLevel(bool success, string reason)
    {
        if (string.IsNullOrEmpty(currentLevelId)) return;

        float levelDuration = Time.time - levelStartTime;
        float accuracy = CalculateAccuracy() * 100f;

        var levelCompletionData = new Dictionary<string, object>
        {
            { "endedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "levelDuration", levelDuration },
            { "completionReason", reason },
            { "correctMatches", levelCorrectMatches },
            { "errors", levelErrors },
            { "accuracy", accuracy }
        };

        string path = $"users/{username}/dragGame/parentSessions/{parentSessionId}/levels/{currentLevelId}";
        dbRef.Child(path).UpdateChildrenAsync(levelCompletionData);

        if (success)
        {
            UpdateParentSessionCounters();
            
            Debug.Log($"Nivel completado - Acertos: {levelCorrectMatches}, Erros: {levelErrors}, Tempo: {levelDuration:F1}s");
            OnLevelCompleted?.Invoke(currentLevelId, levelCorrectMatches, currentLevelIndex);
        }
        else
        {
            Debug.Log($"Nivel falhou - Razao: {reason}, Acertos: {levelCorrectMatches}, Erros: {levelErrors}");
        }
    }
    
    public void LogDragEnd(string imageType, Vector2 startPosition, Vector2 endPosition, bool wasDropped, float dragTime, float dragDistance)
    {
        if (!isDragging) return;

        isDragging = false;

        Debug.Log($"Drag finalizado: {imageType} - " +
                $"Tempo de arrasto: {dragTime:F2}s, " +
                $"Distancia: {dragDistance:F2}, " +
                $"Drop: {wasDropped}, " +
                $"Start: {startPosition}, " +
                $"End: {endPosition}");

        var dragData = new Dictionary<string, object>
        {
            { "imageType", imageType },
            { "dragTime", dragTime },
            { "dragDistance", dragDistance },
            { "wasDropped", wasDropped },
            { "startPosition", $"{startPosition.x:F2},{startPosition.y:F2}" },
            { "endPosition", $"{endPosition.x:F2},{endPosition.y:F2}" },
            { "timestamp", DateTime.Now.ToString("HH:mm:ss") }
        };

        string path = $"users/{username}/dragGame/parentSessions/{parentSessionId}/levels/{currentLevelId}/dragEvents";
        dbRef.Child(path).Push().SetValueAsync(dragData);
    }

    private void CompleteParentSession()
    {
        float totalDuration = Time.time - parentSessionStartTime;
        float overallAccuracy = CalculateOverallAccuracy() * 100f;

        var sessionCompletionData = new Dictionary<string, object>
        {
            { "endedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "totalDuration", totalDuration },
            { "levelsCompleted", totalLevelsCompleted },
            { "totalCorrectMatches", totalCorrectMatches },
            { "totalErrors", totalErrors },
            { "overallAccuracy", overallAccuracy }
        };

        dbRef.Child($"users/{username}/dragGame/parentSessions/{parentSessionId}")
            .UpdateChildrenAsync(sessionCompletionData);

        Debug.Log($"Sessao Pai finalizada - Tempo total: {totalDuration:F1}s, Acertos: {totalCorrectMatches}, Erros: {totalErrors}, Precisao: {overallAccuracy:F1}%");
    }

    // ===== MÉTODOS AUXILIARES =====
    private void UpdateLevelCounters()
    {
        var counters = new Dictionary<string, object>
        {
            { "currentCorrectMatches", levelCorrectMatches },
            { "currentErrors", levelErrors },
            { "currentAccuracy", CalculateAccuracy() * 100f }
        };

        string path = $"users/{username}/dragGame/parentSessions/{parentSessionId}/levels/{currentLevelId}";
        dbRef.Child(path).UpdateChildrenAsync(counters);
    }

    private void UpdateParentSessionCounters()
    {
        var sessionCounters = new Dictionary<string, object>
        {
            { "totalCorrectMatches", totalCorrectMatches },
            { "totalErrors", totalErrors },
            { "levelsCompleted", totalLevelsCompleted },
            { "overallAccuracy", CalculateOverallAccuracy() * 100f }
        };

        dbRef.Child($"users/{username}/dragGame/parentSessions/{parentSessionId}")
            .UpdateChildrenAsync(sessionCounters);
    }

    private float CalculateAccuracy()
    {
        int totalAttempts = levelCorrectMatches + levelErrors;
        return totalAttempts > 0 ? (float)levelCorrectMatches / totalAttempts : 0f;
    }

    private float CalculateOverallAccuracy()
    {
        int totalAttempts = totalCorrectMatches + totalErrors;
        return totalAttempts > 0 ? (float)totalCorrectMatches / totalAttempts : 0f;
    }

    private int GetLevelNumber()
    {
        return (currentLevelIndex - FIRST_LEVEL_INDEX) + 1;
    }

    // ===== INFO PÚBLICA =====
    public int GetCurrentLevelIndex() => currentLevelIndex;
    public string GetCurrentLevelName() => SceneManager.GetActiveScene().name;
    public int GetCurrentLevelNumber() => GetLevelNumber();
    public int GetTotalLevels() => TOTAL_LEVELS;
    public bool IsLastLevel() => currentLevelIndex >= LAST_LEVEL_INDEX;
}