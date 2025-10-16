using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;

public class CardGameLogger : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string username;
    private string currentSessionId;

    private float sessionStartTime;
    private float roundStartTime;
    private int totalTouches = 0;
    private int correctMatches = 0;
    private int wrongMatches = 0;
    private int roundsCompleted = 0;
    private int currentRound = 0;

    private void Start()
    {
        Debug.Log("CARTAO GAME LOGGER INICIADO");
        
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
        currentRound = 0;

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}";
        
        var sessionData = new Dictionary<string, object>
        {
            { "iniciadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(sessionData).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Sessao Card Game criada no Firebase: " + currentSessionId);
            }
            else
            {
                Debug.LogError("Erro ao criar sessao: " + task.Exception);
            }
        });

        Debug.Log("Sessao Card Game iniciada: " + currentSessionId);
    }

    public void StartRound(int roundNumber, int pairsCount)
    {
        currentRound = roundNumber;
        roundStartTime = Time.time;

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}/rounds/{roundNumber}";
        
        var roundData = new Dictionary<string, object>
        {
            { "iniciadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "quantidadePares", pairsCount },
            { "numeroRound", roundNumber }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(roundData);

        Debug.Log("Round " + roundNumber + " iniciado - " + pairsCount + " pares");
    }

    public void LogCardTouch(int cardIndex, Vector2 position, string cardName, bool isFirstCard)
    {
        totalTouches++;

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}/toques";
        
        var touchData = new Dictionary<string, object>
        {
            { "indiceCarta", cardIndex },
            { "posicaoX", position.x },
            { "posicaoY", position.y },
            { "nomeCarta", cardName },
            { "primeiraCarta", isFirstCard },
            { "round", currentRound },
            { "totalToques", totalTouches },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        dbRef.Child(path).Push().SetValueAsync(touchData);

        Debug.Log("Toque " + totalTouches + " - Carta: " + cardName + ", Posicao: (" + position.x.ToString("F1") + ", " + position.y.ToString("F1") + "), Round: " + currentRound);
    }

    public void LogCardMatch(bool wasCorrect, string cardName1, string cardName2, float matchTime, int attempts)
    {
        if (wasCorrect)
        {
            correctMatches++;
            Debug.Log("MATCH CORRETO: " + cardName1 + " + " + cardName2 + " | Tempo: " + matchTime.ToString("F1") + "s");
        }
        else
        {
            wrongMatches++;
            Debug.Log("MATCH ERRADO: " + cardName1 + " + " + cardName2 + " | Tentativas: " + attempts);
        }

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}/matches";
        
        var matchData = new Dictionary<string, object>
        {
            { "correto", wasCorrect },
            { "carta1", cardName1 },
            { "carta2", cardName2 },
            { "tempoMatch", matchTime },
            { "tentativas", attempts },
            { "round", currentRound },
            { "totalCorretos", correctMatches },
            { "totalErrados", wrongMatches },
            { "acuracia", CalcularTaxaAcuracia() },
            { "timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        
        dbRef.Child(path).Push().SetValueAsync(matchData);
    }

    public void LogRoundComplete(int roundNumber, int pairsCount, float roundDuration)
    {
        roundsCompleted++;
        float roundTime = Time.time - roundStartTime;

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}/rounds/{roundNumber}";
        
        var roundData = new Dictionary<string, object>
        {
            { "completadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "duracaoRound", roundTime },
            { "paresCompletados", pairsCount },
            { "toquesEsteRound", totalTouches },
            { "matchesCorretosEsteRound", correctMatches },
            { "matchesErradosEsteRound", wrongMatches },
            { "acuraciaEsteRound", CalcularTaxaAcuracia() }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(roundData);

        Debug.Log("Round " + roundNumber + " completo! Duracao: " + roundTime.ToString("F1")  + ", Acertos: " + correctMatches + "/" + (correctMatches + wrongMatches));
    }

    public void LogSessionEnd(int finalScore)
    {
        Debug.Log("LOG SESSION END CARD GAME");
        
        float sessionEndTime = Time.time;
        float totalPlayTime = sessionEndTime - sessionStartTime;

        string path = $"users/{username}/cardGame/sessions/{currentSessionId}";
        
        var endData = new Dictionary<string, object>
        {
            { "finalizadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            { "tempoTotalJogo", totalPlayTime },
            { "roundsCompletados", roundsCompleted },
            { "totalToques", totalTouches },
            { "totalMatchesCorretos", correctMatches },
            { "totalMatchesErrados", wrongMatches },
            { "acuraciaFinal", CalcularTaxaAcuracia() },
            { "status", "completado" }
        };
        
        dbRef.Child(path).UpdateChildrenAsync(endData).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Sessao Card Game finalizada!");
                Debug.Log("Estatisticas Finais:");
                Debug.Log("   Tempo Total: " + totalPlayTime.ToString("F1") + "s");
                Debug.Log("   Rounds: " + roundsCompleted);
                Debug.Log("   Toques: " + totalTouches);
                Debug.Log("   Acertos: " + correctMatches);
                Debug.Log("   Erros: " + wrongMatches);
                Debug.Log("   Acuracia: " + CalcularTaxaAcuracia() + "%");
            }
        });
    }

    private int CalcularTaxaAcuracia()
    {
        int totalMatches = correctMatches + wrongMatches;
        if (totalMatches > 0)
        {
            float accuracy = (float)correctMatches / totalMatches * 100f;
            return Mathf.RoundToInt(accuracy);
        }
        return 0;
    }


    private void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit - Finalizando sessao Card Game...");
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy - Finalizando sessao Card Game...");
    }
}