using UnityEngine;
using TMPro; // Para usar o texto da UI
using Vosk; // O namespace principal do plugin

public class ControladorDeTeste_Vosk : MonoBehaviour
{
    [Header("Referências da Cena")]
    [Tooltip("Arraste para cá o componente 'VoskSpeechToText' do seu objeto 'ControladorDeTeste'")]
    public VoskSpeechToText VoskSpeechToText;

    [Tooltip("Arraste para cá o objeto de Texto da UI onde o resultado vai aparecer")]
    public TMP_Text textoResultado;

    void Start()
    {
        // Garante que temos as referências
        if (VoskSpeechToText == null)
        {
            Debug.LogError("O componente VoskSpeechToText não foi atribuído no Inspector!");
            return;
        }

        // Inscreve a nossa função para ser chamada quando houver um resultado
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
        
        // Coloca um texto inicial
        if(textoResultado != null)
        {
            textoResultado.text = "Pressione o botão e fale...";
        }
    }

    /// <summary>
    /// Esta é a função que o seu BOTÃO na UI deve chamar.
    /// </summary>
    public void OnBotaoGravarClicado()
    {
        // O nome correto da função dentro de VoskSpeechToText.cs é "ToggleRecording"
        VoskSpeechToText.ToggleRecording();
    }

    /// <summary>
    /// Esta função é chamada automaticamente pelo plugin quando ele reconhece uma fala.
    /// </summary>
    private void OnTranscriptionResult(string resultadoJson)
    {
        Debug.Log("Vosk retornou: " + resultadoJson);
        if (textoResultado == null) return;

        // O plugin já nos dá a classe "RecognitionResult" para traduzir o JSON
        var resultado = new RecognitionResult(resultadoJson);

        // O resultado pode ter várias frases alternativas.
        // Vamos pegar a primeira, que é a mais provável.
        if (resultado.Phrases.Length > 0)
        {
            textoResultado.text = resultado.Phrases[0].Text;
        }
    }

    // Boa prática: remover a inscrição ao sair da cena para evitar erros.
    void OnDestroy()
    {
        if (VoskSpeechToText != null)
        {
            VoskSpeechToText.OnTranscriptionResult -= OnTranscriptionResult;
        }
    }
}