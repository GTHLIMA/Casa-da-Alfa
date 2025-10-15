using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class DragGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int loadCount = 0;
    private int correctMatches = 0;
    private int errors = 0;

    private void Start()
    {
        if (FirebaseUserSession.Instance == null || string.IsNullOrEmpty(FirebaseUserSession.Instance.LoggedUser))
        {
            Debug.LogError("Usuário não logado no DragGameLogger!");
            return;
        }

        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/dragGame/loadCount";

        dbRef.Child(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
                int.TryParse(task.Result.Value.ToString(), out loadCount);

            loadCount++;
            dbRef.Child(path).SetValueAsync(loadCount);

            Debug.Log($"[DragGame] LoadCount -> {loadCount}");
            UnityMainThreadDispatcher.Enqueue(StartNewSession);
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/dragGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt").SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        correctMatches = 0;
        errors = 0;

        Debug.Log($"[DragGame] Sessão iniciada -> {currentSessionId}");
    }

    public float EndSession()
    {
        float sessionEndTime = Time.time;
        float sessionDuration = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/dragGame/sessions/{currentSessionId}";
        dbRef.Child(path).UpdateChildrenAsync(new Dictionary<string, object>
        {
            { "endedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "sessionDuration", sessionDuration }
        });

        Debug.Log($"[DragGame] Sessão finalizada: {currentSessionId} | Duração: {sessionDuration:F2}s");
        return sessionDuration;
    }

    public void CompleteCurrentLevel(bool success, int matchesCompleted = 0, string reason = "completed")
    {
        if (matchesCompleted > 0)
        {
            correctMatches = matchesCompleted;
            UpdateStats();
        }

        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        string path = $"users/{username}/dragGame/sessions/{currentSessionId}/levelCompletion";

        var completionData = new Dictionary<string, object>
        {
            { "success", success },
            { "matchesCompleted", matchesCompleted },
            { "reason", reason },
            { "totalCorrectMatches", correctMatches },
            { "totalErrors", errors },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(completionData);
        Debug.Log(success
            ? $"[DragGame] Nível completado com sucesso! Acertos: {correctMatches}, Erros: {errors}"
            : $"[DragGame] Nível falhou ({reason}) Acertos: {correctMatches}, Erros: {errors}");
    }

    public void LogCorrectMatch(string imageType, string targetType)
    {
        correctMatches++;
        UpdateStats();
        LogMatchEvent("correct", imageType, targetType);
    }

    public void LogError(string expectedType, string receivedType, string errorType)
    {
        errors++;
        UpdateStats();
        LogMatchEvent("error", expectedType, receivedType, errorType);
    }

    private void LogMatchEvent(string type, string a, string b, string extra = null)
    {
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        string path = $"users/{username}/dragGame/sessions/{currentSessionId}/matches";

        var data = new Dictionary<string, object>
        {
            { "type", type },
            { "a", a },
            { "b", b },
            { "extra", extra },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(data);
        Debug.Log($"[DragGame] {type.ToUpper()}: {a} -> {b} | Taxa: {CalculateAccuracyRate():P0}");
    }

    public void LogDragStart(string imageType, Vector2 position)
    {
        LogDragEvent("start", imageType, position, Vector2.zero);
    }

    public void LogDragEnd(string imageType, Vector2 startPos, Vector2 endPos, bool wasDropped, float dragTime, float dragDistance)
    {
        LogDragEvent("end", imageType, startPos, endPos, wasDropped, dragTime, dragDistance);
    }

    private void LogDragEvent(string phase, string imageType, Vector2 startPos, Vector2 endPos, bool wasDropped = false, float dragTime = 0f, float dragDistance = 0f)
    {
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        string path = $"users/{username}/dragGame/sessions/{currentSessionId}/dragEvents";

        var dragData = new Dictionary<string, object>
        {
            { "phase", phase },
            { "imageType", imageType },
            { "wasDropped", wasDropped },
            { "dragTime", dragTime },
            { "dragDistance", dragDistance },
            { "start", $"{startPos.x:F2},{startPos.y:F2}" },
            { "end", $"{endPos.x:F2},{endPos.y:F2}" },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(dragData);
        Debug.Log($"[DragGame] Drag {phase}: {imageType}");
    }

    private void UpdateStats()
    {
        var stats = new Dictionary<string, object>
        {
            { "correctMatches", correctMatches },
            { "errors", errors },
            { "accuracyRate", CalculateAccuracyRate() }
        };

        string path = $"users/{username}/dragGame/sessions/{currentSessionId}/stats";
        dbRef.Child(path).UpdateChildrenAsync(stats);
    }

    private float CalculateAccuracyRate()
    {
        int total = correctMatches + errors;
        return total > 0 ? (float)correctMatches / total : 0f;
    }

    public void RestartCurrentLevel(string reason = "restart") => CompleteCurrentLevel(false, 0, reason);
    public void FailCurrentLevel(string reason = "failed") => CompleteCurrentLevel(false, 0, reason);

    private void OnApplicationQuit() => EndSession();
    private void OnDestroy() => EndSession();
}
