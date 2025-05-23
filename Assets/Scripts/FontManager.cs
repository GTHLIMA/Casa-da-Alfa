using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FontManager : MonoBehaviour
{
    public static FontManager Instance;

    public TMP_FontAsset fontBastao;
    public TMP_FontAsset fontImprensa;
    public TMP_FontAsset fontCurva;

    private TMP_FontAsset fonteAtual;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Função que será chamada quando uma nova cena for carregada
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFontToAllText();
    }

    public void SetFont(int index)
    {
        switch(index)
        {
            case 0:
                fonteAtual = fontBastao;
                break;
            case 1:
                fonteAtual = fontImprensa;
                break;
            case 2:
                fonteAtual = fontCurva;
                break;
        }

        ApplyFontToAllText();
    }

    public void ApplyFontToAllText()
    {
        if (fonteAtual == null)
            return;

        TextMeshProUGUI[] textosUGUI = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI texto in textosUGUI)
        {
            texto.font = fonteAtual;
            texto.ForceMeshUpdate();
        }

        TextMeshPro[] textos3D = FindObjectsOfType<TextMeshPro>(true);
        foreach (TextMeshPro texto in textos3D)
        {
            texto.font = fonteAtual;
            texto.ForceMeshUpdate();
        }
    }

    public TMP_FontAsset GetCurrentFont()
    {
        return fonteAtual;
    }
}
