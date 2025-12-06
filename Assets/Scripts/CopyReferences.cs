using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script auxiliar para copiar referências do ImageVoiceMatcher para TrainWhisperGameManager
/// USAR APENAS UMA VEZ, depois pode deletar este script
/// </summary>
#if UNITY_EDITOR
public class CopyReferences : MonoBehaviour
{
    [Header("⚠️ USAR APENAS UMA VEZ!")]
    [Tooltip("Componente ANTIGO (ImageVoiceMatcher)")]
    public ImageVoiceMatcher oldComponent;
    
    [Tooltip("Componente NOVO (TrainWhisperGameManager)")]
    public TrainWhisperGameManager newComponent;

    [ContextMenu("🔄 COPIAR TODAS AS REFERÊNCIAS")]
    public void CopyAllReferences()
    {
        if (oldComponent == null)
        {
            Debug.LogError("❌ 'Old Component' não atribuído!");
            return;
        }

        if (newComponent == null)
        {
            Debug.LogError("❌ 'New Component' não atribuído!");
            return;
        }

        Debug.Log("🔄 Iniciando cópia de referências...");

        // Configuração Central
        newComponent.vowelIndexToPlay = oldComponent.vowelIndexToPlay;
        
        // ⚠️ All Vowel Data precisa ser copiado MANUALMENTE porque são tipos diferentes!
        Debug.LogWarning("⚠️ COPIE MANUALMENTE: All Vowel Data (arraste do componente antigo para o novo)");

        // Interface do Microfone
        newComponent.micIndicatorImage = oldComponent.micIndicatorImage;
        newComponent.micIndicatorAnimator = oldComponent.micIndicatorAnimator;
        Debug.Log("✅ Interface do Microfone copiada");

        // Cores
        newComponent.promptingColor = oldComponent.promptingColor;
        newComponent.listeningColor = oldComponent.listeningColor;
        newComponent.staticColor = oldComponent.staticColor;
        Debug.Log("✅ Cores copiadas");

        // Áudios
        newComponent.standardPrompt = oldComponent.standardPrompt;
        newComponent.variablePrompts = oldComponent.variablePrompts;
        newComponent.congratulatoryAudio = oldComponent.congratulatoryAudio;
        newComponent.supportAudios = oldComponent.supportAudios;
        Debug.Log("✅ Áudios copiados");

        // Efeitos
        newComponent.endOfLevelConfetti = oldComponent.endOfLevelConfetti;
        Debug.Log("✅ Efeitos copiados");

        // Tempos
        newComponent.initialDelay = oldComponent.initialDelay;
        newComponent.delayAfterCorrect = oldComponent.delayAfterCorrect;
        newComponent.delayAfterHint = oldComponent.delayAfterHint;
        newComponent.delayAfterPromptBeforeReveal = oldComponent.delayAfterPromptBeforeReveal;
        Debug.Log("✅ Tempos copiados");

        // Trem
        newComponent.trainController = oldComponent.trainController;
        Debug.Log("✅ Train Controller copiado");

        // UI Score
        newComponent.scorePause = oldComponent.scorePause;
        newComponent.scoreEndPhase = oldComponent.scoreEndPhase;
        newComponent.scoreHUD = oldComponent.scoreHUD;
        newComponent.PauseMenu = oldComponent.PauseMenu;
        Debug.Log("✅ UI Score copiada");

        // Marca como modificado para salvar
        EditorUtility.SetDirty(newComponent);

        Debug.Log("🎉 REFERÊNCIAS COPIADAS COM SUCESSO!");
        Debug.Log("");
        Debug.Log("⚠️ CONFIGURE MANUALMENTE ESTES CAMPOS:");
        Debug.Log("   1. ⭐ ALL VOWEL DATA (IMPORTANTE!)");
        Debug.Log("      → Arraste do ImageVoiceMatcher para TrainWhisperGameManager");
        Debug.Log("   2. Whisper Voice (novo campo)");
        Debug.Log("   3. End Phase Panel");
        Debug.Log("   4. Number Counter");
        Debug.Log("");
        Debug.Log("✅ Depois teste com Play Mode");
        Debug.Log("❌ Se funcionar, remova o ImageVoiceMatcher");
    }

    [ContextMenu("🗑️ LIMPAR COMPONENTES ANTIGOS")]
    public void RemoveOldComponents()
    {
        if (oldComponent != null)
        {
            Debug.Log("🗑️ Removendo ImageVoiceMatcher...");
            DestroyImmediate(oldComponent);
        }

        Debug.Log("🗑️ Removendo CopyReferences...");
        DestroyImmediate(this);
        
        Debug.Log("✅ Limpeza concluída!");
    }
}
#endif