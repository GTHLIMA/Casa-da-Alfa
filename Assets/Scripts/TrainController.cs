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
    public float imageFadeDuration = 0.5f;
    public float delayBeforeReveal = 0.5f;

    // AQUI ESTÁ A LISTA CORRETA PARA AS DISTÂNCIAS VARIADAS
    [Tooltip("A sequência de distâncias que o trem avança. A ordem será repetida.")]
    public float[] stepDistances = new float[8];

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

        foreach (var wagon in wagonImages)
        {
            if (wagon.questionMarkImage != null) wagon.questionMarkImage.enabled = true;
            if (wagon.wordImage != null)
            {
                wagon.wordImage.enabled = false;
                wagon.wordImage.color = new Color(1, 1, 1, 0);
            }
        }
    }

    public IEnumerator AnimateIn(Sprite firstSprite)
    {
        currentWagonIndex = 0;
        rectTransform.anchoredPosition = new Vector2(startX_offscreen, initialYPosition);
        gameObject.SetActive(true);
        if (trainEnteringSound != null) audioSource.PlayOneShot(trainEnteringSound);
        Vector2 targetPosition = new Vector2(firstStopX_onscreen, initialYPosition);
        yield return MoveToPosition(targetPosition, moveDuration);
        yield return new WaitForSeconds(delayBeforeReveal);
        yield return RevealWagonImage(0, firstSprite);
    }

    public IEnumerator AdvanceAndChangeImage(int nextImageIndex, Sprite nextSprite)
    {
        yield return HideWagonImage(currentWagonIndex);
        
        if (stepDistances != null && stepDistances.Length > 0)
        {
            float distanceToMove = stepDistances[currentWagonIndex % stepDistances.Length];
            float newX = rectTransform.anchoredPosition.x + distanceToMove;
            Vector2 targetPosition = new Vector2(newX, initialYPosition);
            if (advanceSound != null) audioSource.PlayOneShot(advanceSound);
            yield return MoveToPosition(targetPosition, moveDuration);
        }

        currentWagonIndex = nextImageIndex;
        yield return new WaitForSeconds(delayBeforeReveal);
        yield return RevealWagonImage(currentWagonIndex, nextSprite);
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
            wagon.questionMarkImage.enabled = false;
        }
        if (wagon.wordImage != null)
        {
            wagon.wordImage.sprite = sprite;
            wagon.wordImage.enabled = true;
            yield return StartCoroutine(Fade(wagon.wordImage, 1f, imageFadeDuration));
        }
    }

    private IEnumerator HideWagonImage(int index)
    {
        if (index < 0 || index >= wagonImages.Count) yield break;
        WagonImageGroup wagon = wagonImages[index];
        if (wagon.wordImage != null)
        {
            yield return StartCoroutine(Fade(wagon.wordImage, 0f, imageFadeDuration));
            wagon.wordImage.enabled = false;
        }
        if (wagon.questionMarkImage != null)
        {
            wagon.questionMarkImage.enabled = true;
            yield return StartCoroutine(Fade(wagon.questionMarkImage, 1f, imageFadeDuration));
        }
    }

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