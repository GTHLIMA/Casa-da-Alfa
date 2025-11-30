using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class PuzzleSyllableGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private int loadCount = 0;
    private int piecesPlaced = 0;
    private int piecesMisplaced = 0;
    private int puzzlesCompleted = 0;
    private int totalInteractions = 0;

    private void Start()
    {
        if (FirebaseUserSession.Instance == null || string.IsNullOrEmpty(FirebaseUserSession.Instance.LoggedUser))
        {
            Debug.LogError("Usuário não logado no PuzzleSyllableGameLogger!");
            return;
        }

        username = FirebaseUserSession.Instance.LoggedUser;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        LoadAndIncrementLoadCount();
    }

    private void LoadAndIncrementLoadCount()
    {
        string path = $"users/{username}/puzzleSyllableGame/loadCount";

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

            Debug.Log("PuzzleSyllableGame - LoadCount atualizado -> " + loadCount);
            UnityMainThreadDispatcher.Enqueue(() => StartNewSession());
        });
    }

    private void StartNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        sessionStartTime = Time.time;

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}";
        dbRef.Child(path).Child("startedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        piecesPlaced = 0;
        piecesMisplaced = 0;
        puzzlesCompleted = 0;
        totalInteractions = 0;

        Debug.Log("PuzzleSyllableGame - Sessão iniciada: " + currentSessionId);
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

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}";
        
        dbRef.Child(path).Child("endedAt")
            .SetValueAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        
        dbRef.Child(path).Child("sessionDuration")
            .SetValueAsync(sessionDuration);

        UpdateStats();

        Debug.Log($"PuzzleSyllableGame - Sessão finalizada: {currentSessionId}");
        Debug.Log($"Duração: {sessionDuration:F2}s | Peças colocadas: {piecesPlaced} | Puzzles: {puzzlesCompleted}");

        currentSessionId = null;
        return sessionDuration;
    }

    public void LogPiecePickup(string syllable, int syllableIndex, int pieceIndex, Vector2 piecePosition)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;

        totalInteractions++;

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}/interactions";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        var interactionData = new Dictionary<string, object>
        {
            { "type", "pickup" },
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "pieceIndex", pieceIndex },
            { "positionX", piecePosition.x },
            { "positionY", piecePosition.y },
            { "totalInteractions", totalInteractions },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(interactionData);

        Debug.Log($"PuzzleSyllableGame - Peça {pieceIndex} pega: {syllable} | Posição: ({piecePosition.x:F1}, {piecePosition.y:F1})");
    }

    public void LogPiecePlacement(string syllable, int syllableIndex, int pieceIndex, Vector2 piecePosition, bool correct, float placementTime)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;

        totalInteractions++;
        if (correct)
            piecesPlaced++;
        else
            piecesMisplaced++;

        UpdateStats();

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}/interactions";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        var interactionData = new Dictionary<string, object>
        {
            { "type", "placement" },
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "pieceIndex", pieceIndex },
            { "correct", correct },
            { "positionX", piecePosition.x },
            { "positionY", piecePosition.y },
            { "placementTime", placementTime },
            { "accuracyRate", CalculateAccuracyRate() },
            { "totalInteractions", totalInteractions },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(interactionData);

        Debug.Log($"PuzzleSyllableGame - Peça {pieceIndex} colocada: {syllable} | Correto: {correct} | Tempo: {placementTime:F2}s");
    }

    public void LogPuzzleCompleted(string syllable, int syllableIndex, int totalPieces, float completionTime)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;

        puzzlesCompleted++;

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}/puzzles";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        var puzzleData = new Dictionary<string, object>
        {
            { "syllable", syllable },
            { "syllableIndex", syllableIndex },
            { "totalPieces", totalPieces },
            { "completionTime", completionTime },
            { "puzzlesCompleted", puzzlesCompleted },
            { "accuracyRate", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(puzzleData);

        Debug.Log($"PuzzleSyllableGame - Puzzle completo: {syllable} | Peças: {totalPieces} | Tempo: {completionTime:F2}s");
    }

    public void LogGameCompleted(int totalSyllables, int totalPiecesPlaced, float totalPlayTime)
    {
        if (string.IsNullOrEmpty(currentSessionId)) return;

        string path = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}/gameCompletion";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var completionData = new Dictionary<string, object>
        {
            { "totalSyllables", totalSyllables },
            { "totalPiecesPlaced", totalPiecesPlaced },
            { "totalPlayTime", totalPlayTime },
            { "puzzlesCompleted", puzzlesCompleted },
            { "finalAccuracy", CalculateAccuracyRate() },
            { "timestamp", timestamp }
        };

        dbRef.Child(path).Push().SetValueAsync(completionData);

        Debug.Log($"PuzzleSyllableGame - Jogo completo! Puzzles: {puzzlesCompleted}/{totalSyllables} | Precisão: {CalculateAccuracyRate():P0}");
    }

    private void UpdateStats()
    {
        float accuracyRate = CalculateAccuracyRate();

        var statsPath = $"users/{username}/puzzleSyllableGame/sessions/{currentSessionId}/stats";
        var statsData = new Dictionary<string, object>
        {
            { "piecesPlaced", piecesPlaced },
            { "piecesMisplaced", piecesMisplaced },
            { "puzzlesCompleted", puzzlesCompleted },
            { "totalInteractions", totalInteractions },
            { "accuracyRate", accuracyRate }
        };

        dbRef.Child(statsPath).UpdateChildrenAsync(statsData);
    }

    private float CalculateAccuracyRate()
    {
        int totalPlacements = piecesPlaced + piecesMisplaced;
        return totalPlacements > 0 ? (float)piecesPlaced / totalPlacements : 0f;
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