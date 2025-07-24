using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Classe interna para agrupar as duas imagens de cada vagão.
[System.Serializable]
public class WagonImageGroup
{
    public Image questionMarkImage;
    public Image wordImage;
}

[RequireComponent(typeof(AudioSource))]
public class TrainController : MonoBehaviour
{
    [Header("Referências dos Vagões")]
    [Tooltip("Lista de TODOS os pares de imagens de cada vagão, em ordem.")]
    public List<WagonImageGroup> wagonImages; // MUDANÇA: Agora é uma lista de grupos de imagens

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
    private int currentWagonIndex = -1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
        initialYPosition = rectTransform.anchoredPosition.y;

        // Garante que no início, só as interrogações apareçam.
        foreach (var wagon in wagonImages)
        {
            if (wagon.questionMarkImage != null) wagon.questionMarkImage.enabled = true;
            if (wagon.wordImage != null)
            {
                wagon.wordImage.enabled = false;
                wagon.wordImage.color = new Color(1, 1, 1, 0); // Começa transparente
            }
        }
    }

    public IEnumerator AnimateIn(Sprite firstSprite)
    {
        rectTransform.anchoredPosition = new Vector2(startX_offscreen, initialYPosition);
        gameObject.SetActive(true);

        if (trainEnteringSound != null) audioSource.PlayOneShot(trainEnteringSound);

        Vector2 targetPosition = new Vector2(firstStopX_onscreen, initialYPosition);
        yield return MoveToPosition(targetPosition, moveDuration);

        // Revela a imagem do primeiro vagão (índice 0)
        yield return RevealWagonImage(0, firstSprite);
    }

    public IEnumerator AdvanceAndChangeImage(int nextImageIndex, Sprite nextSprite)
    {
        // Esconde a imagem do vagão anterior ao começar a avançar
        yield return HideWagonImage(currentWagonIndex);

        currentWagonIndex = nextImageIndex;
        float targetX = firstStopX_onscreen + (currentWagonIndex * stepDistance);
        Vector2 targetPosition = new Vector2(targetX, initialYPosition);

        if (advanceSound != null) audioSource.PlayOneShot(advanceSound);
        yield return MoveToPosition(targetPosition, moveDuration);

        // Revela a nova imagem no vagão atual
        yield return RevealWagonImage(currentWagonIndex, nextSprite);
    }

    // Corrotina para mover o trem
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

    // Corrotina que faz a "mágica" da revelação
    private IEnumerator RevealWagonImage(int index, Sprite sprite)
    {
        if (index < 0 || index >= wagonImages.Count) yield break;
        WagonImageGroup wagon = wagonImages[index];

        // 1. Some com a interrogação
        if (wagon.questionMarkImage != null)
        {
            yield return StartCoroutine(Fade(wagon.questionMarkImage, 0f, imageFadeDuration));
            wagon.questionMarkImage.enabled = false;
        }

        // 2. Aparece com a imagem do desenho
        if (wagon.wordImage != null)
        {
            wagon.wordImage.sprite = sprite;
            wagon.wordImage.enabled = true;
            yield return StartCoroutine(Fade(wagon.wordImage, 1f, imageFadeDuration));
        }
    }

    // Corrotina que reseta o vagão para o estado inicial (mostrando a interrogação)
    private IEnumerator HideWagonImage(int index)
    {
        if (index < 0 || index >= wagonImages.Count) yield break;
        WagonImageGroup wagon = wagonImages[index];

        // 1. Some com a imagem do desenho
        if (wagon.wordImage != null)
        {
            yield return StartCoroutine(Fade(wagon.wordImage, 0f, imageFadeDuration));
            wagon.wordImage.enabled = false;
        }

        // 2. Reaparece com a interrogação
        if (wagon.questionMarkImage != null)
        {
            wagon.questionMarkImage.enabled = true;
            // Opcional: fade-in para a interrogação também
            yield return StartCoroutine(Fade(wagon.questionMarkImage, 1f, imageFadeDuration)); 
        }
    }

    // Corrotina genérica para controlar o fade de qualquer imagem
    private IEnumerator Fade(Image image, float targetAlpha, float duration)
    {
        if (image == null) yield break;
        float elapsedTime = 0f;
        Color startColor = image.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        while (elapsedTime < duration)
        {
            image.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        image.color = endColor;
    }
}