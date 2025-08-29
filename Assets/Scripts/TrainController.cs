using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    public List<WagonImageGroup> wagonImages;

    [Header("Configurações de Movimento")]
    public float moveDuration = 1.5f;
    public float startX_offscreen = -1500f;
    public float firstStopX_onscreen = 0f;
    public float[] stepDistances = new float[8];
    public float imageFadeDuration = 0.5f;
    [Tooltip("Tempo (em segundos) após o início do movimento para tocar o áudio da pergunta.")]
    public float promptAudioDelay = 0.5f;

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
        HideAllWordImages();
    }

    public IEnumerator AnimateIn(AudioClip promptAudio)
    {
        currentWagonIndex = 0;
        HideAllWordImages();
        rectTransform.anchoredPosition = new Vector2(startX_offscreen, initialYPosition);
        gameObject.SetActive(true);
        
        if (trainEnteringSound != null) audioSource.PlayOneShot(trainEnteringSound);
        
        Vector2 targetPosition = new Vector2(firstStopX_onscreen, initialYPosition);
        yield return MoveAndPlayAudio(targetPosition, moveDuration, promptAudio, promptAudioDelay);
    }

    public IEnumerator AdvanceToNextWagon(int nextImageIndex, AudioClip promptAudio)
    {
        HideAllWordImages(); 
        if (stepDistances.Length > 0)
        {
            float distanceToMove = stepDistances[currentWagonIndex % stepDistances.Length];
            float newX = rectTransform.anchoredPosition.x + distanceToMove;
            Vector2 targetPosition = new Vector2(newX, initialYPosition);

            if (advanceSound != null) audioSource.PlayOneShot(advanceSound);

            yield return MoveAndPlayAudio(targetPosition, moveDuration, promptAudio, promptAudioDelay);
        }
        currentWagonIndex = nextImageIndex;
    }

    public IEnumerator RevealCurrentImage(Sprite sprite)
    {
        yield return RevealWagonImage(currentWagonIndex, sprite);
    }

    // MÉTODO DE MOVIMENTO ATUALIZADO PARA CONTROLAR O ÁUDIO
    private IEnumerator MoveAndPlayAudio(Vector2 targetPosition, float duration, AudioClip clipToPlay, float delay)
    {
        float elapsedTime = 0f;
        Vector2 startingPosition = rectTransform.anchoredPosition;
        bool audioHasPlayed = false;

        while (elapsedTime < duration)
        {
            // Movimenta o trem
            rectTransform.anchoredPosition = Vector2.Lerp(startingPosition, targetPosition, elapsedTime / duration);
            
            // Verifica se já passou o tempo do delay e se o áudio ainda não tocou
            if (!audioHasPlayed && elapsedTime >= delay)
            {
                if (clipToPlay != null)
                {
                    audioSource.PlayOneShot(clipToPlay);
                }
                audioHasPlayed = true;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Garante que o trem pare na posição exata
        rectTransform.anchoredPosition = targetPosition;
    }

    // --- ESTE É O MÉTODO QUE ESTAVA FALTANDO ---
    private void HideAllWordImages()
    {
        foreach (var wagon in wagonImages)
        {
            if (wagon.questionMarkImage != null)
            {
                wagon.questionMarkImage.enabled = true;
                wagon.questionMarkImage.color = new Color(1, 1, 1, 1);
            }
            if (wagon.wordImage != null)
            {
                wagon.wordImage.enabled = false;
                wagon.wordImage.color = new Color(1, 1, 1, 0);
            }
        }
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

    private IEnumerator RevealWagonImage(int index, Sprite sprite)
    {
        if (index < 0 || index >= wagonImages.Count) yield break;
        WagonImageGroup wagon = wagonImages[index];
        if (wagon.questionMarkImage != null)
        {
            yield return StartCoroutine(Fade(wagon.questionMarkImage, 0f, imageFadeDuration));
        }
        if (wagon.wordImage != null)
        {
            wagon.wordImage.sprite = sprite;
            wagon.wordImage.enabled = true;
            yield return StartCoroutine(Fade(wagon.wordImage, 1f, imageFadeDuration));
        }
    }

    private IEnumerator Fade(Image image, float targetAlpha, float duration)
    {
        if (image == null) yield break;

        // Ativa a imagem antes do fade-out para garantir que a cor possa ser alterada
        if(targetAlpha < 1f) image.enabled = true;

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

        // Desativa a imagem apenas se o fade for para ficar invisível
        if(targetAlpha == 0f) image.enabled = false;
    }
}