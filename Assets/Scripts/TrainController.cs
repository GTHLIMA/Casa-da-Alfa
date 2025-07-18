using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Necessário para usar List<>

[RequireComponent(typeof(AudioSource))]
public class TrainController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("A lista de TODOS os componentes Image de cada vagão, em ordem.")]
    public List<Image> wagonImageContainers; // MUDANÇA: Agora é uma lista

    [Header("Configurações de Movimento")]
    public float moveDuration = 1.5f;
    public float startX_offscreen = -1500f;
    public float firstStopX_onscreen = 0f;
    public float stepDistance = 200f;
    public float imageFadeDuration = 0.5f;

    [Header("Áudios do Trem")]
    public AudioClip trainEnteringSound;
    public AudioClip advanceSound;

    private RectTransform rectTransform;
    private AudioSource audioSource;
    private float initialYPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
        initialYPosition = rectTransform.anchoredPosition.y;

        // Desativa todas as imagens no início para garantir que comecem limpas
        foreach (var container in wagonImageContainers)
        {
            if (container != null) container.enabled = false;
        }
    }

    public IEnumerator AnimateIn(Sprite firstSprite)
    {
        rectTransform.anchoredPosition = new Vector2(startX_offscreen, initialYPosition);
        gameObject.SetActive(true);

        if (trainEnteringSound != null) audioSource.PlayOneShot(trainEnteringSound);

        Vector2 targetPosition = new Vector2(firstStopX_onscreen, initialYPosition);
        yield return MoveToPosition(targetPosition, moveDuration);

        // Mostra a primeira imagem no primeiro vagão (índice 0)
        yield return StartCoroutine(FadeImage(0, firstSprite, 1f));
        
        Debug.Log("[TrainController] - Trem chegou à primeira parada.");
    }

    public IEnumerator AdvanceAndChangeImage(int nextImageIndex, Sprite nextSprite)
    {
        // 1. Apaga a imagem do vagão ANTERIOR
        if (nextImageIndex > 0 && nextImageIndex <= wagonImageContainers.Count)
        {
            yield return StartCoroutine(FadeImage(nextImageIndex - 1, null, 0f));
        }

        // 2. Avança o trem
        float newX = rectTransform.anchoredPosition.x + stepDistance;
        Vector2 targetPosition = new Vector2(newX, initialYPosition);

        if (advanceSound != null) audioSource.PlayOneShot(advanceSound);
        yield return MoveToPosition(targetPosition, moveDuration);

        // 3. Mostra a nova imagem no vagão ATUAL
        yield return StartCoroutine(FadeImage(nextImageIndex, nextSprite, 1f));
        
        Debug.Log($"[TrainController] - Trem avançou para o vagão #{nextImageIndex}.");
    }

    private IEnumerator MoveToPosition(Vector2 targetPosition, float duration)
    {
        float elapsedTime = 0f;
        Vector2 startingPosition = rectTransform.anchoredPosition;
        while (elapsedTime < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startingPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = targetPosition;
    }

    // MUDANÇA: Agora o método FadeImage precisa saber QUAL imagem controlar
    private IEnumerator FadeImage(int containerIndex, Sprite newSprite, float targetAlpha)
    {
        if (containerIndex < 0 || containerIndex >= wagonImageContainers.Count || wagonImageContainers[containerIndex] == null)
        {
            Debug.LogError($"Índice de vagão inválido ou não configurado: {containerIndex}");
            yield break;
        }

        Image targetImage = wagonImageContainers[containerIndex];

        // Se um novo sprite for fornecido, atualiza
        if (newSprite != null)
        {
            targetImage.sprite = newSprite;
        }
        
        targetImage.enabled = true;
        float elapsedTime = 0f;
        Color startColor = targetImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        
        while (elapsedTime < imageFadeDuration)
        {
            targetImage.color = Color.Lerp(startColor, endColor, elapsedTime / imageFadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetImage.color = endColor;

        if (targetAlpha == 0f)
        {
            targetImage.enabled = false;
        }
    }
}