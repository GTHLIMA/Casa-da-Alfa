using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;


public class FontManager : MonoBehaviour
{
    public static FontManager Instance;

    public TMP_FontAsset fontBastao;
    public TMP_FontAsset fontImprensa;
    public TMP_FontAsset fontCurva;

    private Dictionary<TMP_Text, string> textosOriginais = new Dictionary<TMP_Text, string>();


    private TMP_FontAsset fonteAtual;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            fonteAtual = fontBastao; // Define Bastão como padrão

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

        TMP_Text[] textos = FindObjectsOfType<TMP_Text>(true);
        
        foreach (TMP_Text texto in textos)
        {
            if (texto.CompareTag("IgnoreFontChange"))
                continue;

            // Salva o texto original se ainda não tiver salvo
            if (!textosOriginais.ContainsKey(texto))
                textosOriginais[texto] = texto.text;

            // Aplica a fonte
            texto.font = fonteAtual;

            // Aplica o tamanho conforme a fonte
            if (fonteAtual == fontBastao)
            {
                texto.text = textosOriginais[texto].ToUpper();
                texto.fontSize = 56f;
            }
            else
            {
                texto.text = textosOriginais[texto];
                texto.fontSize = 100f;
            }

            texto.ForceMeshUpdate();
        }
    }


    public TMP_FontAsset GetCurrentFont()
    {
        return fonteAtual;
    }
}
