using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gerenciador de reconhecimento de voz usando Whisper API da OpenAI
/// Grava áudio do microfone, trata (remove silêncio/normaliza) e envia para transcrição.
/// </summary>
public class WhisperVoiceRecognition : MonoBehaviour
{
    [Header("🎤 Configurações de Gravação")]
    [Tooltip("Tempo máximo de gravação em segundos")]
    public float maxRecordingTime = 5f;
    
    [Tooltip("Frequência de amostragem (16000 é ideal para Whisper, 44100 para alta qualidade)")]
    public int sampleRate = 16000; 

    [Header("🔑 API Configuration")]
    [Tooltip("OPÇÃO A: URL do seu backend (ex: https://seuservidor.com/api/transcribe)")]
    public string backendURL = "";
    
    [Tooltip("OPÇÃO B: API Key da OpenAI (NÃO RECOMENDADO para produção!)")]
    public string openAIKey = "";
    
    [Tooltip("Usar backend (true) ou API direta (false)")]
    public bool useBackend = true;

    [Header("🎯 Configurações de Validação")]
    [Tooltip("Número máximo de tentativas antes de falhar")]
    public int maxAttemptsBeforeReset = 3;
    
    [Tooltip("Prompt para o Whisper (ajuda na precisão)")]
    public string whisperPrompt = "Transcreva em português brasileiro exatamente como o som, mesmo que sejam sílabas curtas como BA, CA, DA, FA, GA, LA, MA, etc.";

    [Header("🔊 Audio Source (opcional)")]
    [Tooltip("AudioSource para tocar dicas após erros")]
    public AudioSource hintAudioSource;

    // Eventos
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;
    public event Action<string> OnTranscriptionReceived;
    public event Action<bool> OnValidationComplete;

    // Variáveis privadas
    private AudioClip recordedClip;
    private string currentDeviceName;
    private bool isRecording = false;
    private bool isProcessing = false;
    
    private string expectedWord;
    private Action<bool> callbackWhenDone;
    private int attemptCount = 0;
    private bool isListening = false;

  private void Start()
    {
        // Verifica microfone
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[WhisperVoice] ❌ Nenhum microfone detectado!");
            return;
        }
        currentDeviceName = Microphone.devices[0];
        
        // --- LÓGICA DE CARREGAMENTO DA CHAVE ---
        
        // 1. Se não estamos usando Backend (usando API direta)
        // 2. E a chave no Inspector está vazia
        if (!useBackend && string.IsNullOrEmpty(openAIKey))
        {
            // Tenta carregar do GameSecrets (agora funciona na Build também)
            try 
            {
                // Verifica se a classe existe via Reflection (para evitar erro de compilação se o arquivo sumir)
                // OU se você tem certeza que o arquivo existe, use direto: openAIKey = GameSecrets.OPENAI_KEY;
                
                // Vamos assumir que você tem o arquivo. Se der erro de compilação, avise.
                openAIKey = GameSecrets.OPENAI_KEY;
                Debug.Log("[WhisperVoice] 🔑 Chave carregada do GameSecrets via código.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WhisperVoice] ❌ Erro ao carregar GameSecrets: {e.Message}");
            }
        }

        // --- DEBUG DA CHAVE (MOSTRA SÓ O COMEÇO E O FIM) ---
        string keyStatus = "VAZIA";
        if (!string.IsNullOrEmpty(openAIKey))
        {
            if (openAIKey.Length > 8)
                keyStatus = $"{openAIKey.Substring(0, 4)}...{openAIKey.Substring(openAIKey.Length - 4)}";
            else
                keyStatus = "CURTA_DEMAIS";
        }

        Debug.Log($"[WhisperVoice] 🛡️ STATUS DA CHAVE NO START: [{keyStatus}]");
        // ----------------------------------------------------

        if (useBackend && string.IsNullOrEmpty(backendURL))
        {
            Debug.LogError("[WhisperVoice] ❌ Backend URL vazia!");
        }
        else if (!useBackend && string.IsNullOrEmpty(openAIKey))
        {
            Debug.LogError("[WhisperVoice] ❌ Chave OpenAI VAZIA! O reconhecimento vai falhar com 401.");
        }
    }

    /// <summary>
    /// Inicia o processo de escuta com tentativas
    /// </summary>
    public void StartListening(string expected, Action<bool> callback)
    {
        if (isListening)
        {
            Debug.LogWarning("[WhisperVoice] Já está escutando. Ignorando.");
            return;
        }

        expectedWord = expected;
        callbackWhenDone = callback;
        attemptCount = 0;
        isListening = true;

        Debug.Log($"[WhisperVoice] 🎯 Iniciando escuta para: '{expected}'");
        StartCoroutine(ListenCycle());
    }

    /// <summary>
    /// Para o processo de escuta
    /// </summary>
    public void StopListening()
    {
        isListening = false;
        StopRecording();
        StopAllCoroutines();
        Debug.Log("[WhisperVoice] ⏹️ Escuta interrompida");
    }

    /// <summary>
    /// Ciclo de tentativas de reconhecimento
    /// </summary>
    private IEnumerator ListenCycle()
    {
        while (attemptCount < maxAttemptsBeforeReset && isListening)
        {
            attemptCount++;
            Debug.Log($"[WhisperVoice] 📢 Tentativa {attemptCount}/{maxAttemptsBeforeReset}");

            yield return null;

            bool recordingStarted = StartRecording();
            if (!recordingStarted)
            {
                Debug.LogError("[WhisperVoice] ❌ Falha ao iniciar gravação");
                FinishWithResult(false);
                yield break;
            }

            // Aguarda o tempo de gravação
            float elapsed = 0f;
            while (elapsed < maxRecordingTime && isRecording)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            StopRecording();

            // Processa o áudio gravado
            yield return StartCoroutine(ProcessRecordedAudio());

            if (!isListening) yield break; // Se acertou, sai.

            // Se errou e ainda tem tentativas, espera um pouco e tenta de novo
            if (attemptCount < maxAttemptsBeforeReset)
            {
                PlayHintForAttempt(attemptCount);
                yield return new WaitForSeconds(1f);
            }
        }

        if (isListening)
        {
            Debug.Log($"[WhisperVoice] ⚠️ Esgotou {maxAttemptsBeforeReset} tentativas.");
            FinishWithResult(false);
        }
    }

    private bool StartRecording()
    {
        if (isRecording) return false;

        try
        {
            if (recordedClip != null)
            {
                Destroy(recordedClip);
                recordedClip = null;
            }

            recordedClip = Microphone.Start(currentDeviceName, false, (int)maxRecordingTime + 1, sampleRate);
            isRecording = true;
            
            OnRecordingStarted?.Invoke();
            Debug.Log($"[WhisperVoice] 🔴 Gravação iniciada ({maxRecordingTime}s)");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WhisperVoice] Erro ao iniciar gravação: {e.Message}");
            return false;
        }
    }

    private void StopRecording()
    {
        if (!isRecording) return;
        Microphone.End(currentDeviceName);
        isRecording = false;
        OnRecordingStopped?.Invoke();
        Debug.Log("[WhisperVoice] Gravação parada");
    }

    private IEnumerator ProcessRecordedAudio()
    {
        if (recordedClip == null)
        {
            Debug.LogError("[WhisperVoice] Nenhum áudio gravado!");
            yield break;
        }

        isProcessing = true;
        Debug.Log("[WhisperVoice] Processando áudio...");

        // Converte AudioClip para WAV com TRATAMENTO (Normalização + Corte)
        byte[] wavData = ConvertAudioClipToWav(recordedClip);
        
        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogWarning("[WhisperVoice] Áudio muito curto ou vazio após tratamento (possível silêncio).");
            isProcessing = false;
            // Não conta como erro de API, mas retorna falso para tentar de novo ou falhar a tentativa
            yield break;
        }

        Debug.Log($"[WhisperVoice] Áudio pronto para envio: {wavData.Length} bytes");

        if (useBackend)
        {
            yield return StartCoroutine(SendToBackend(wavData));
        }
        else
        {
            yield return StartCoroutine(SendToWhisperAPI(wavData));
        }

        isProcessing = false;
    }

    // --- MÉTODOS DE API ---

    private IEnumerator SendToBackend(byte[] wavData)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavData, "audio.wav", "audio/wav"),
            new MultipartFormDataSection("prompt", whisperPrompt)
        };

        using (UnityWebRequest request = UnityWebRequest.Post(backendURL, formData))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                ProcessTranscription(response);
            }
            else
            {
                Debug.LogError($"[WhisperVoice] Erro Backend: {request.error}");
            }
        }
    }

    private IEnumerator SendToWhisperAPI(byte[] wavData)
    {
        string url = "https://api.openai.com/v1/audio/transcriptions";
        
        // --- DEBUG DE SEGURANÇA ANTES DE ENVIAR ---
        string debugKey = "NULA";
        if (!string.IsNullOrEmpty(openAIKey) && openAIKey.Length > 10)
        {
            // Mostra: sk-p...A9z1 
            debugKey = $"{openAIKey.Substring(0, 4)}...{openAIKey.Substring(openAIKey.Length - 4)}";
        }
        Debug.Log($"[WhisperVoice] Enviando para API. Chave usada: [{debugKey}] | Tamanho áudio: {wavData.Length} bytes");
        // ------------------------------------------

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavData, "audio.wav", "audio/wav"),
            new MultipartFormDataSection("model", "whisper-1"),
            new MultipartFormDataSection("language", "pt"),
            new MultipartFormDataSection("prompt", whisperPrompt)
        };

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            // AQUI É ONDE OCORRE O ERRO 401 SE A CHAVE ESTIVER ERRADA
            request.SetRequestHeader("Authorization", $"Bearer {openAIKey}");
            request.timeout = 30;
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                try
                {
                    WhisperResponse whisperResponse = JsonUtility.FromJson<WhisperResponse>(response);
                    ProcessTranscription(whisperResponse.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WhisperVoice] Erro JSON: {e.Message}");
                }
            }
            else
            {
                // LOG DETALHADO DO ERRO
                Debug.LogError($"[WhisperVoice] Erro API ({request.responseCode}): {request.error}");
                Debug.LogError($"[WhisperVoice] Retorno do Servidor: {request.downloadHandler.text}");
                
                if (request.responseCode == 401)
                {
                    Debug.LogError("[WhisperVoice] ERRO 401: NÃO AUTORIZADO. A chave está vazia, incorreta ou você está sem créditos na OpenAI.");
                }
            }
        }
    }

    private void ProcessTranscription(string transcription)
    {
        if (string.IsNullOrEmpty(transcription)) return;

        transcription = transcription.Trim();
        Debug.Log($"[WhisperVoice] Transcrição: '{transcription}'");
        
        OnTranscriptionReceived?.Invoke(transcription);

        bool isCorrect = CheckMatch(expectedWord, transcription);
        
        if (isCorrect)
        {
            FinishWithResult(true);
        }
        else
        {
            Debug.Log($"[WhisperVoice] Não bateu: Esperado '{expectedWord}' vs Recebido '{transcription}'");
        }
    }

    // --- TRATAMENTO DE ÁUDIO (MELHORIA PARA CELULAR) ---

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        if (clip == null) return null;

        try
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 1. Corte de Silêncio
            float[] trimmedSamples = TrimSilence(samples, 0.01f);

            if (trimmedSamples.Length == 0) return null;

            // 2. Normalização (Aumento de Volume)
            NormalizeAudio(trimmedSamples);

            // 3. Conversão para PCM 16-bit
            short[] intData = new short[trimmedSamples.Length];
            byte[] bytesData = new byte[trimmedSamples.Length * 2];
            float rescaleFactor = 32767; 

            for (int i = 0; i < trimmedSamples.Length; i++)
            {
                intData[i] = (short)(trimmedSamples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            int hz = clip.frequency;
            int channels = clip.channels;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytesData.Length);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(hz);
                writer.Write(hz * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(bytesData.Length);
                writer.Write(bytesData);

                return stream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WhisperVoice] Erro WAV: {e.Message}");
            return null;
        }
    }

    private void NormalizeAudio(float[] samples)
    {
        float max = 0f;
        for (int i = 0; i < samples.Length; i++)
            if (Mathf.Abs(samples[i]) > max) max = Mathf.Abs(samples[i]);

        if (max < 0.001f) return; 

        float gain = 0.99f / max;
        for (int i = 0; i < samples.Length; i++)
            samples[i] *= gain;
            
        Debug.Log($"[WhisperVoice] Normalizado (Ganho: {gain:F1}x)");
    }

    private float[] TrimSilence(float[] samples, float threshold)
    {
        int startIndex = 0;
        int endIndex = samples.Length - 1;

        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > threshold) { startIndex = i; break; }
        }

        for (int i = samples.Length - 1; i >= 0; i--)
        {
            if (Mathf.Abs(samples[i]) > threshold) { endIndex = i; break; }
        }

        if (startIndex >= endIndex) return new float[0];

        int padding = (int)(sampleRate * 0.2f); // 0.2s de margem
        startIndex = Mathf.Max(0, startIndex - padding);
        endIndex = Mathf.Min(samples.Length - 1, endIndex + padding);

        int length = endIndex - startIndex + 1;
        float[] result = new float[length];
        Array.Copy(samples, startIndex, result, 0, length);

        return result;
    }

    // --- HELPERS E CLASSES AUXILIARES ---

    private bool CheckMatch(string expected, string received)
    {
        if (string.IsNullOrEmpty(received)) return false;
        string exp = NormalizeText(expected);
        string rec = NormalizeText(received);
        if (exp == rec) return true;
        int distance = LevenshteinDistance(exp, rec);
        int tolerance = Mathf.Max(1, expected.Length / 3);
        return distance <= tolerance;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string normalized = RemoveAccents(text).ToUpper().Trim();
        return normalized;
    }

    private string RemoveAccents(string text)
    {
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length; int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        if (n == 0) return m; if (m == 0) return n;
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int j = 1; j <= m; j++)
            for (int i = 1; i <= n; i++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        return d[n, m];
    }

    private void PlayHintForAttempt(int attempt)
    {
        var mm = MainGameManager.Instance;
        if (mm != null && mm.syllableSource != null)
        {
             var currentData = mm.syllables[mm.currentSyllableIndex];
             if (currentData.syllableClip != null) mm.syllableSource.PlayOneShot(currentData.syllableClip);
        }
    }

    private void FinishWithResult(bool success)
    {
        isListening = false;
        StopAllCoroutines();
        OnValidationComplete?.Invoke(success);
        callbackWhenDone?.Invoke(success);
    }

    private void OnDestroy()
    {
        StopListening();
        if (recordedClip != null) Destroy(recordedClip);
    }

    // ✅✅✅ A CLASSE QUE ESTAVA FALTANDO ESTÁ AQUI EMBAIXO:
    [System.Serializable]
    private class WhisperResponse
    {
        public string text;
    }
}