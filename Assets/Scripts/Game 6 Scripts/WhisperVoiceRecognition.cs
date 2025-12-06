using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Gerenciador de reconhecimento de voz usando Whisper API da OpenAI
/// Grava áudio do microfone e envia para transcrição
/// </summary>
public class WhisperVoiceRecognition : MonoBehaviour
{
    [Header("🎤 Configurações de Gravação")]
    [Tooltip("Tempo máximo de gravação em segundos")]
    public float maxRecordingTime = 5f;
    
    [Tooltip("Frequência de amostragem (22050 ou 44100 recomendado)")]
    public int sampleRate = 22050;

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
        // Verifica se há microfone disponível
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[WhisperVoice] ❌ Nenhum microfone detectado!");
            return;
        }

        currentDeviceName = Microphone.devices[0];
        Debug.Log($"[WhisperVoice] 🎤 Microfone detectado: {currentDeviceName}");

        // --- MODIFICAÇÃO DE SEGURANÇA (Adicione isso) ---
        // Se estiver no Editor, não usando Backend, e a chave estiver vazia no Inspector:
#if UNITY_EDITOR
        if (!useBackend && string.IsNullOrEmpty(openAIKey))
        {
            // Pega a chave do arquivo secreto que o Git ignora
            openAIKey = GameSecrets.OPENAI_KEY;
            Debug.Log("[WhisperVoice] 🔑 Usando chave segura do GameSecrets.cs");
        }
#endif
        // ------------------------------------------------

        // Validação de configuração
        if (useBackend && string.IsNullOrEmpty(backendURL))
        {
            Debug.LogError("[WhisperVoice] ❌ Backend URL não configurada! Configure no Inspector.");
        }
        else if (!useBackend && string.IsNullOrEmpty(openAIKey))
        {
            Debug.LogError("[WhisperVoice] ❌ OpenAI API Key não configurada! Configure no Inspector ou no GameSecrets.cs.");
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

            // Aguarda um frame antes de começar
            yield return null;

            // Inicia gravação
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

            // Para gravação
            StopRecording();

            // Processa o áudio gravado
            yield return StartCoroutine(ProcessRecordedAudio());

            // Se acertou, termina
            if (!isListening)
            {
                yield break;
            }

            // Se errou e ainda tem tentativas, espera um pouco
            if (attemptCount < maxAttemptsBeforeReset)
            {
                PlayHintForAttempt(attemptCount);
                yield return new WaitForSeconds(1f);
            }
        }

        // Esgotou tentativas
        if (isListening)
        {
            Debug.Log($"[WhisperVoice] ⚠️ Esgotou {maxAttemptsBeforeReset} tentativas.");
            FinishWithResult(false);
        }
    }

    /// <summary>
    /// Inicia a gravação do microfone
    /// </summary>
    private bool StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("[WhisperVoice] Já está gravando!");
            return false;
        }

        try
        {
            // Limpa gravação anterior
            if (recordedClip != null)
            {
                Destroy(recordedClip);
                recordedClip = null;
            }

            // Inicia gravação
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

    /// <summary>
    /// Para a gravação do microfone
    /// </summary>
    private void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(currentDeviceName);
        isRecording = false;
        
        OnRecordingStopped?.Invoke();
        Debug.Log("[WhisperVoice] ⏹️ Gravação parada");
    }

    /// <summary>
    /// Processa o áudio gravado e envia para o Whisper
    /// </summary>
    private IEnumerator ProcessRecordedAudio()
    {
        if (recordedClip == null)
        {
            Debug.LogError("[WhisperVoice] ❌ Nenhum áudio gravado!");
            yield break;
        }

        isProcessing = true;
        Debug.Log("[WhisperVoice] 🔄 Processando áudio...");

        // Converte AudioClip para WAV
        byte[] wavData = ConvertAudioClipToWav(recordedClip);
        
        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogError("[WhisperVoice] ❌ Falha ao converter áudio para WAV");
            isProcessing = false;
            yield break;
        }

        Debug.Log($"[WhisperVoice] 📦 Áudio convertido: {wavData.Length} bytes");

        // Envia para transcrição
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

    /// <summary>
    /// Envia áudio para backend intermediário (RECOMENDADO)
    /// </summary>
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
            
            Debug.Log($"[WhisperVoice] 📤 Enviando para backend: {backendURL}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"[WhisperVoice] ✅ Resposta do backend: {response}");
                
                ProcessTranscription(response);
            }
            else
            {
                Debug.LogError($"[WhisperVoice] ❌ Erro no backend: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// Envia áudio diretamente para API da OpenAI (NÃO RECOMENDADO PARA PRODUÇÃO)
    /// </summary>
    private IEnumerator SendToWhisperAPI(byte[] wavData)
    {
        string url = "https://api.openai.com/v1/audio/transcriptions";
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavData, "audio.wav", "audio/wav"),
            new MultipartFormDataSection("model", "whisper-1"),
            new MultipartFormDataSection("language", "pt"),
            new MultipartFormDataSection("prompt", whisperPrompt)
        };

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            request.SetRequestHeader("Authorization", $"Bearer {openAIKey}");
            request.timeout = 30;
            
            Debug.Log("[WhisperVoice] 📤 Enviando para Whisper API...");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"[WhisperVoice] ✅ Resposta da API: {response}");
                
                // Parse JSON response
                try
                {
                    WhisperResponse whisperResponse = JsonUtility.FromJson<WhisperResponse>(response);
                    ProcessTranscription(whisperResponse.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WhisperVoice] ❌ Erro ao parsear JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[WhisperVoice] ❌ Erro na API: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// Processa a transcrição recebida
    /// </summary>
    private void ProcessTranscription(string transcription)
    {
        if (string.IsNullOrEmpty(transcription))
        {
            Debug.LogWarning("[WhisperVoice] ⚠️ Transcrição vazia");
            return;
        }

        transcription = transcription.Trim();
        Debug.Log($"[WhisperVoice] 📝 Transcrição: '{transcription}'");
        
        OnTranscriptionReceived?.Invoke(transcription);

        // Valida se está correto
        bool isCorrect = CheckMatch(expectedWord, transcription);
        
        if (isCorrect)
        {
            Debug.Log($"[WhisperVoice] ✅ CORRETO! '{transcription}' == '{expectedWord}'");
            FinishWithResult(true);
        }
        else
        {
            Debug.Log($"[WhisperVoice] ❌ INCORRETO: '{transcription}' != '{expectedWord}' (Tentativa {attemptCount}/{maxAttemptsBeforeReset})");
        }
    }

    /// <summary>
    /// Valida se a palavra falada corresponde à esperada
    /// </summary>
    private bool CheckMatch(string expected, string received)
    {
        if (string.IsNullOrEmpty(received)) return false;

        // Normaliza ambas as strings
        string exp = NormalizeText(expected);
        string rec = NormalizeText(received);

        Debug.Log($"[WhisperVoice] 🔍 Comparando: '{exp}' com '{rec}'");

        // Match exato
        if (exp == rec) return true;

        // Tolerância com Levenshtein distance
        int distance = LevenshteinDistance(exp, rec);
        int tolerance = Mathf.Max(1, expected.Length / 3); // 33% de tolerância
        
        bool match = distance <= tolerance;
        Debug.Log($"[WhisperVoice] 📏 Distance: {distance}, Tolerance: {tolerance}, Match: {match}");
        
        return match;
    }

    /// <summary>
    /// Normaliza texto: remove acentos, maiúsculas, espaços
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        string normalized = RemoveAccents(text);
        normalized = normalized.ToUpper().Trim();
        
        return normalized;
    }

    /// <summary>
    /// Remove acentos de uma string
    /// </summary>
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

    /// <summary>
    /// Calcula distância de Levenshtein entre duas strings
    /// </summary>
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

    /// <summary>
    /// Toca dica após erro
    /// </summary>
    private void PlayHintForAttempt(int attempt)
    {
        var mm = MainGameManager.Instance;
        if (mm == null || mm.syllableSource == null) return;

        var currentData = mm.syllables[mm.currentSyllableIndex];
        
        if (currentData.syllableClip != null)
        {
            Debug.Log($"[WhisperVoice] 💡 Dica: tocando sílaba novamente (tentativa {attempt})");
            mm.syllableSource.PlayOneShot(currentData.syllableClip);
        }
    }

    /// <summary>
    /// Finaliza o processo com resultado
    /// </summary>
    private void FinishWithResult(bool success)
    {
        isListening = false;
        StopAllCoroutines();
        
        OnValidationComplete?.Invoke(success);
        callbackWhenDone?.Invoke(success);
        
        Debug.Log($"[WhisperVoice] 🏁 Resultado final: {(success ? "✅ APROVADO" : "❌ REPROVADO")}");
    }

    /// <summary>
    /// Converte AudioClip para formato WAV
    /// </summary>
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        if (clip == null) return null;

        try
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Converte para 16-bit PCM
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            float rescaleFactor = 32767; // para 16 bit

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            // Cria header WAV
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples_count = samples.Length;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytesData.Length);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // chunk size
                writer.Write((short)1); // audio format (PCM)
                writer.Write((short)channels);
                writer.Write(hz);
                writer.Write(hz * channels * 2); // byte rate
                writer.Write((short)(channels * 2)); // block align
                writer.Write((short)16); // bits per sample

                // data chunk
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(bytesData.Length);
                writer.Write(bytesData);

                return stream.ToArray();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WhisperVoice] Erro ao converter áudio: {e.Message}");
            return null;
        }
    }

    private void OnDestroy()
    {
        StopListening();
        
        if (recordedClip != null)
        {
            Destroy(recordedClip);
        }
    }

    // Classe auxiliar para parsear JSON da API
    [System.Serializable]
    private class WhisperResponse
    {
        public string text;
    }
}