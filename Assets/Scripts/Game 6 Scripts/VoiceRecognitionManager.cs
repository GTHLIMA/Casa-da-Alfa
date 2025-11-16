using System;
using System.Collections;
using UnityEngine;

public class VoiceRecognitionManager : MonoBehaviour, ISpeechToTextListener
{
    [Header("Settings")]
    public int maxAttemptsBeforeReset = 3; 
    public float listeningTimeout = 5f;
    public float delayBetweenAttempts = 0.5f;

    private int attemptCount = 0;
    private string expectedWord;
    private Action<bool> callbackWhenDone;
    private bool isListening = false;
    private bool resultReceived = false;

    public void StartListening(string expected, Action<bool> callback)
    {
        if (isListening)
        {
            Debug.LogWarning("[VoiceRecognition] Já está escutando. Ignorando.");
            return;
        }

        expectedWord = expected;
        callbackWhenDone = callback;
        attemptCount = 0;
        isListening = true;
        resultReceived = false;

        StartCoroutine(ListenCycle());
    }

    IEnumerator ListenCycle()
    {
        while (attemptCount < maxAttemptsBeforeReset && isListening)
        {
            resultReceived = false;
            attemptCount++;

            Debug.Log($"[VoiceRecognition] Tentativa {attemptCount}/{maxAttemptsBeforeReset}");

            bool sttStarted = StartSTT();
            
            if (!sttStarted)
            {
                Debug.LogError("[VoiceRecognition] Falha ao iniciar STT");
                FinishWithResult(false);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < listeningTimeout && !resultReceived && isListening)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!resultReceived && isListening)
            {
                Debug.Log("[VoiceRecognition] Timeout - nenhum resultado recebido.");
                PlayHintForAttempt(attemptCount);
                yield return new WaitForSeconds(delayBetweenAttempts);
            }
            else if (resultReceived)
            {
                yield break;
            }
        }

        if (isListening)
        {
            Debug.Log($"[VoiceRecognition] ⚠️ Esgotou {maxAttemptsBeforeReset} tentativas.");
            FinishWithResult(false);
        }
    }

    private bool StartSTT()
    {
#if UNITY_ANDROID
        try
        {
            SpeechToText.Start(this, true, false);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoiceRecognition] Erro ao iniciar STT: {e.Message}");
            return false;
        }
#else
        Debug.LogWarning("[VoiceRecognition] STT disponível apenas no Android. Use hotkeys C/X no Editor.");
        return true;
#endif
    }

    public void OnResultReceived(string recognizedText, int? errorCode)
    {
        if (!isListening) return;

        resultReceived = true;

        if (errorCode.HasValue && errorCode.Value != 0)
        {
            Debug.LogWarning($"[VoiceRecognition] Erro STT: {errorCode}");
            return;
        }

        Debug.Log($"[VoiceRecognition] Reconhecido: '{recognizedText}' | Esperado: '{expectedWord}'");

        bool correct = CheckMatch(expectedWord, recognizedText);
        
        if (correct)
        {
            Debug.Log("[VoiceRecognition] ✅ ACERTOU!");
            FinishWithResult(true);
        }
        else
        {
            Debug.Log($"[VoiceRecognition] ❌ ERROU (tentativa {attemptCount}/{maxAttemptsBeforeReset})");
        }
    }

    private void FinishWithResult(bool success)
    {
        isListening = false;
        StopAllCoroutines();
        callbackWhenDone?.Invoke(success);
    }

    private bool CheckMatch(string expected, string received)
    {
        if (string.IsNullOrEmpty(received)) return false;

        string exp = NormalizeText(expected);
        string rec = NormalizeText(received);

        Debug.Log($"[VoiceRecognition] Comparando: '{exp}' com '{rec}'");

        if (exp == rec) return true;

        int distance = LevenshteinDistance(exp, rec);
        int tolerance = Mathf.Max(1, expected.Length / 3);
        
        bool match = distance <= tolerance;
        Debug.Log($"[VoiceRecognition] Distance: {distance}, Tolerance: {tolerance}, Match: {match}");
        
        return match;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        string withoutAccents = RemoveAccents(text);
        string normalized = withoutAccents.ToUpper();
        normalized = normalized.Trim();
        
        return normalized;
    }

    private void PlayHintForAttempt(int attempt)
    {
        var mm = MainGameManager.Instance;
        if (mm == null || mm.syllableSource == null) return;

        var currentData = mm.syllables[mm.currentSyllableIndex];
        
        if (attempt == 1 && currentData.syllableClip != null)
        {
            Debug.Log("[VoiceRecognition] Dica: tocando sílaba novamente");
            mm.syllableSource.PlayOneShot(currentData.syllableClip);
        }
        else if (attempt >= 2 && currentData.syllableClip != null)
        {
            Debug.Log("[VoiceRecognition] Dica: tocando sílaba (tentativa extra)");
            mm.syllableSource.PlayOneShot(currentData.syllableClip);
        }
    }

    public void OnReadyForSpeech() 
    { 
        Debug.Log("[VoiceRecognition] Pronto para ouvir");
    }
    
    public void OnBeginningOfSpeech() 
    { 
        Debug.Log("[VoiceRecognition] Começou a falar");
    }
    
    public void OnVoiceLevelChanged(float level) { }
    
    public void OnPartialResultReceived(string partialText) 
    { 
        Debug.Log($"[VoiceRecognition] Parcial: {partialText}");
    }

    private string RemoveAccents(string text)
    {
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        foreach (char c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != 
                System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int j = 1; j <= m; j++)
        {
            for (int i = 1; i <= n; i++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }

    public void StopListening()
    {
        isListening = false;
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopListening();
    }
}