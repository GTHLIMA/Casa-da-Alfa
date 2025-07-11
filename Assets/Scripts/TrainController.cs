using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Este script não precisa mais de um Animator, mas ainda pode ter um AudioSource.
[RequireComponent(typeof(AudioSource))] 
public class TrainController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O componente 'Image' no trem que vai mostrar a figura da palavra.")]
    public Image imageContainer;

    [Header("Configurações de Movimento")]
    [Tooltip("Duração, em segundos, da animação de entrada e saída.")]
    public float moveDuration = 2.0f;
    [Tooltip("Posição X inicial, fora da tela à esquerda.")]
    public float startX_offscreen = -1500f;
    [Tooltip("Posição X final, no centro da tela.")]
    public float targetX_onscreen = 0f;
    [Tooltip("Posição X de saída, fora da tela à direita.")]
    public float endX_offscreen = 1500f;

    [Header("Áudios do Trem")]
    public AudioClip trainEnteringSound;
    public AudioClip trainExitingSound;
    
    private RectTransform rectTransform;
    private AudioSource audioSource;
    private Vector2 initialPosition;
    public AnimationCurve moveCurve;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
        // Guarda a posição Y inicial para que o trem não suba ou desça.
        initialPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Move o trem para a tela com a imagem correta.
    /// </summary>
    public IEnumerator AnimateIn(Sprite spriteToShow)
    {
        // 1. Define a imagem e a posição inicial fora da tela.
        if (imageContainer != null)
        {
            imageContainer.sprite = spriteToShow;
            imageContainer.enabled = true;
        }
        rectTransform.anchoredPosition = new Vector2(startX_offscreen, initialPosition.y);
        gameObject.SetActive(true);

        // 2. Toca o som de entrada.
        if (trainEnteringSound != null)
        {
            audioSource.PlayOneShot(trainEnteringSound);
        }

        // 3. Move suavemente para a posição alvo.
        Vector2 targetPosition = new Vector2(targetX_onscreen, initialPosition.y);
        yield return MoveToPosition(targetPosition, moveDuration);

        Debug.Log("[TrainController] - Animação de ENTRADA completa.");
    }

    /// <summary>
    /// Move o trem para fora da tela.
    /// </summary>
    public IEnumerator AnimateOut()
    {
        // 1. Toca o som de saída.
        if (trainExitingSound != null)
        {
            audioSource.PlayOneShot(trainExitingSound);
        }

        // 2. Move suavemente para fora da tela.
        Vector2 targetPosition = new Vector2(endX_offscreen, initialPosition.y);
        yield return MoveToPosition(targetPosition, moveDuration);

        // 3. Desativa o objeto e a imagem.
        if (imageContainer != null)
        {
            imageContainer.enabled = false;
        }
        gameObject.SetActive(false);
        Debug.Log("[TrainController] - Animação de SAÍDA completa.");
    }
    
    /// <summary>
    /// Corrotina genérica que move o objeto de sua posição atual para uma posição alvo.
    /// </summary>
   private IEnumerator MoveToPosition(Vector2 targetPosition, float duration)
{
    float elapsedTime = 0f;
    Vector2 startingPosition = rectTransform.anchoredPosition;

    while (elapsedTime < duration)
    {
        // Calcula o progresso linear (de 0.0 a 1.0)
        float progress = elapsedTime / duration;
        
        // Usa a curva para transformar o progresso linear em um progresso "desacelerado"
        float curveValue = moveCurve.Evaluate(progress);

        // Movimento NÃO-LINEAR - usa o valor da curva
        rectTransform.anchoredPosition = Vector2.Lerp(startingPosition, targetPosition, curveValue);
        
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    rectTransform.anchoredPosition = targetPosition;
}
}