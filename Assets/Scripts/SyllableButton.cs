using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SyllableButton : MonoBehaviour
{
    [Header("Referências Internas do Prefab")]
    public Image textImage;
    public Image drawingImage;

    private SyllableBuilderManager manager;
    private SyllableBuilderManager.SyllableButtonData myData;
    private Button button;
    private CanvasGroup canvasGroup;
    
    public float fadeInDuration = 0.3f;

    public void Setup(SyllableBuilderManager.SyllableButtonData data, SyllableBuilderManager managerRef)
    {
        myData = data;
        manager = managerRef;

        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        canvasGroup = GetComponent<CanvasGroup>();
        
        // --- INÍCIO DO CÓDIGO DE DEPURAÇÃO ---
        Debug.Log("--- DEBUG DO BOTÃO: " + gameObject.name + " ---");

        // Teste 1: As referências do prefab estão conectadas no Inspector?
        if (drawingImage == null)
            Debug.LogError("ERRO DE PREFAB: A referência 'Drawing Image' NÃO está conectada no Inspector do prefab 'BotaoSilaba_Modelo'!");
        else
            Debug.Log("SUCESSO: Referência 'Drawing Image' conectada.");

        if (textImage == null)
            Debug.LogError("ERRO DE PREFAB: A referência 'Text Image' NÃO está conectada no Inspector do prefab 'BotaoSilaba_Modelo'!");
        else
            Debug.Log("SUCESSO: Referência 'Text Image' conectada.");

        // Teste 2: Os sprites estão sendo recebidos do SyllableBuilderManager?
        if (data.syllableDrawingImage == null)
            Debug.LogError("ERRO DE DADOS: O campo 'Syllable Drawing Image' NÃO foi definido no Inspector do SyllableBuilderManager para esta sílaba!");
        else
            Debug.Log("SUCESSO: Recebido o sprite do desenho: " + data.syllableDrawingImage.name);

        if (data.syllableTextImage == null)
            Debug.LogError("ERRO DE DADOS: O campo 'Syllable Text Image' NÃO foi definido no Inspector do SyllableBuilderManager para esta sílaba!");
        else
            Debug.Log("SUCESSO: Recebido o sprite do texto: " + data.syllableTextImage.name);
        // --- FIM DO CÓDIGO DE DEPURAÇÃO ---

        // Atribui as imagens recebidas aos componentes corretos
        if (drawingImage != null) drawingImage.sprite = myData.syllableDrawingImage;
        if (textImage != null) textImage.sprite = myData.syllableTextImage;

        if (textImage != null)
        {
            textImage.color = new Color(1, 1, 1, 0);
        }
        
        StartCoroutine(Fade(true, fadeInDuration));
    }

    private void OnClick()
    {
        if (manager != null)
        {
            manager.OnSyllableClicked(myData, this);
        }
    }
    
    public IEnumerator RevealTextImage()
    {
        if (textImage == null) yield break;

        float time = 0f;
        while (time < fadeInDuration)
        {
            textImage.color = new Color(1, 1, 1, Mathf.Lerp(0f, 1f, time / fadeInDuration));
            time += Time.deltaTime;
            yield return null;
        }
        textImage.color = new Color(1, 1, 1, 1);
    }

    public IEnumerator Fade(bool fadeIn, float duration)
    {
        button.interactable = fadeIn;
        float startAlpha = canvasGroup.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0f;
        while (time < duration)
        {
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = endAlpha;
        button.interactable = fadeIn;
    }
}