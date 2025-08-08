using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SyllableButton : MonoBehaviour
{
    private SyllableBuilderManager manager;
    private SyllableBuilderManager.SyllableButtonData myData;
    private Button button;
    private CanvasGroup canvasGroup;
    
    // Duração do fade-in para este botão
    public float fadeInDuration = 0.3f;

    public void Setup(SyllableBuilderManager.SyllableButtonData data, SyllableBuilderManager managerRef)
    {
        myData = data;
        manager = managerRef;

        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        // Pega a referência do CanvasGroup que adicionamos ao prefab
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Inicia o fade-in do botão
        StartCoroutine(Fade(true, fadeInDuration));
    }

    private void OnClick()
    {
        if (manager != null)
        {
            manager.OnSyllableClicked(myData, this);
        }
    }

    // Corrotina para o fade in e out do próprio botão
    public IEnumerator Fade(bool fadeIn, float duration)
    {
        // Garante que o botão seja clicável ou não durante o fade
        button.interactable = fadeIn;

        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float time = 0f;

        canvasGroup.alpha = startAlpha;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}