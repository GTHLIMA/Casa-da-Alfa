using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class BalloonGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private float lastTouchTime;
    private int loadCount = 0;

    private readonly string[] syllables =
    {
        "Ba", "Ca", "Da", "Fa", "Ga", "Ja", "La", "Ma", "Na",
        "Pa", "Qua", "Ra", "Sa", "Ta", "Va", "Xa", "Za"
    };

    private int currentSyllableIndex = 0;

    private void Start()
    {
        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Inicia a contagem de carregamentos e sessão
        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/balloonGame/loadCount";

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

            Debug.Log("LoadCount atualizado -> " + loadCount);
            UnityMainThreadDispatcher.Enqueue(StartNewSession);
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/balloonGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        currentSyllableIndex = 0;
        Debug.Log("Sessão iniciada: " + currentSessionId + " | Tempo: " + sessionStartTime);
    }

    public float EndSession()
    {
        float sessionEndTime = Time.time;
        float sessionDuration = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/balloonGame/sessions/{currentSessionId}";
        
        dbRef.Child(path).Child("endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        dbRef.Child(path).Child("sessionDuration")
            .SetValueAsync(sessionDuration);

        Debug.Log($"Sessão finalizada: {currentSessionId}");
        Debug.Log($"Duração da sessão: {sessionDuration:F2} segundos");

        return sessionDuration;
    }

    public void LogTouch(float yPos)
    {
        float reaction = Time.time - lastTouchTime;
        lastTouchTime = Time.time;

        string path = $"users/{username}/balloonGame/sessions/{currentSessionId}/touches";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        dbRef.Child(path).Child("reactionTimes").Push().SetValueAsync(reaction);

        var positionData = new Dictionary<string, object>
        {
            { "value", yPos },
            { "timestamp", timestamp }
        };
        dbRef.Child(path).Child("yPositions").Push().SetValueAsync(positionData);

        Debug.Log($"Toque registrado | Y: {yPos:F2} | Reação: {reaction:F2}s | {timestamp}");
    }

    public void LogBalloon()
    {
        if (currentSyllableIndex >= syllables.Length)
        {
            Debug.Log("Todas as sílabas já foram usadas nesta sessão.");
            return;
        }

        string syllable = syllables[currentSyllableIndex];
        currentSyllableIndex++;

        string path = $"users/{username}/balloonGame/sessions/{currentSessionId}/balloons";
        dbRef.Child(path).Push().SetValueAsync(syllable);

        Debug.Log("Balão estourado -> " + syllable);
    }
}
