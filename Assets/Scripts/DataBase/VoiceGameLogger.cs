using UnityEngine;
using Firebase.Database;
using System;

public class VoiceGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

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

        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
             .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        correctCount = 0;
        errorCount = 0;
        hintCount = 0;

        Debug.Log("VoiceGame - Sessão iniciada: " + currentSessionId);
    }

    // ====== LOGS ======
    public void LogImageProgress(string word, int index)
    {
        string path = $"users/{username}/voiceGame/sessions/{currentSessionId}/progress";
        dbRef.Child(path).Push().SetRawJsonValueAsync(JsonUtility.ToJson(new
        {
            word = word,
            index = index,
            accuracyRate = CalculateAccuracyRate(),
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }));

        Debug.Log($"Progresso imagem {index} -> {word}, Taxa: {CalculateAccuracyRate():P}");
    }

    public void LogCorrect(string word)
    {
        correctCount++;
        UpdateStats();

        dbRef.Child($"users/{username}/voiceGame/sessions/{currentSessionId}/events").Push()
            .SetRawJsonValueAsync(JsonUtility.ToJson(new
            {
                type = "correct",
                word = word,
                accuracyRate = CalculateAccuracyRate(),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }));

        Debug.Log("Acerto -> " + word + $" | Taxa: {CalculateAccuracyRate():P}");
    }

    public void LogError(string expected, string received)
    {
        errorCount++;
        UpdateStats();

        dbRef.Child($"users/{username}/voiceGame/sessions/{currentSessionId}/events").Push()
            .SetRawJsonValueAsync(JsonUtility.ToJson(new
            {
                type = "error",
                expected = expected,
                received = received,
                accuracyRate = CalculateAccuracyRate(),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }));

        Debug.Log($"Erro -> esperado: {expected}, reconhecido: {received} | Taxa: {CalculateAccuracyRate():P}");
    }

    public void LogHint(string word, int mistakeCount)
    {
        hintCount++;
        dbRef.Child($"users/{username}/voiceGame/sessions/{currentSessionId}/stats/hints")
             .SetValueAsync(hintCount);

        dbRef.Child($"users/{username}/voiceGame/sessions/{currentSessionId}/events").Push()
            .SetRawJsonValueAsync(JsonUtility.ToJson(new
            {
                type = "hint",
                word = word,
                mistakeCount = mistakeCount,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }));

        Debug.Log("Ajuda -> " + word);
    }

    public void LogSessionEnd()
    {
        dbRef.Child($"users/{username}/voiceGame/sessions/{currentSessionId}/endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        Debug.Log("VoiceGame - Sessão finalizada: " + currentSessionId);
    }

    // ====== SUPORTE ======
    private void UpdateStats()
    {
        float accuracyRate = CalculateAccuracyRate();
        int accuracyPercent = Mathf.RoundToInt(accuracyRate * 100f);

        var statsPath = $"users/{username}/voiceGame/sessions/{currentSessionId}/stats";
        dbRef.Child(statsPath).Child("corrects").SetValueAsync(correctCount);
        dbRef.Child(statsPath).Child("errors").SetValueAsync(errorCount);
        dbRef.Child(statsPath).Child("accuracyRate").SetValueAsync(accuracyPercent.ToString() + "%");
    }

    private float CalculateAccuracyRate()
    {
        int total = correctCount + errorCount;
        return total > 0 ? (float)correctCount / total : 0f;
    }

}
