using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class BalloonClickable : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer balloonBodyRenderer;
    public SpriteRenderer innerSyllableRenderer;
    public Image innerSyllableImageUI;

    [Header("Sprites")]
    public Sprite[] balloonPopSteps;
    public Sprite[] syllableStepSprites; // (0 = initial syllable)
    
    [Header("ÁUDIO")]
    public AudioClip popSound;
    public AudioClip[] syllableClickSounds;

    [Header("Pop Animation")]
    public Sprite[] popAnimationFrames;
    public float popFrameRate = 0.06f;
    public float upSpeed = 1.0f;

    [HideInInspector] public int currentStep = 0;
    public event Action onFinalPop;
    public event Action<Vector2> onBalloonPoppedWithPosition;

    private bool isPopping = false;
    private SyllableDado currentSyllableData;

    public void SetSyllableData(SyllableDado syllableData)
    {
        currentSyllableData = syllableData;
        
        if (syllableStepSprites == null || syllableStepSprites.Length == 0)
        {
            syllableStepSprites = new Sprite[] { syllableData.balloonSyllableSprite };
            currentStep = 0;
        }
        else
        {
            syllableStepSprites[0] = syllableData.balloonSyllableSprite;
            currentStep = 0;
        }

        UpdateInnerSprite();
        EnsureSyllableInFront();
    }

    private void Start()
    {
        UpdateInnerSprite();
        EnsureSyllableInFront();
    }
    
    private void EnsureSyllableInFront()
    {
        if (innerSyllableRenderer != null && balloonBodyRenderer != null)
        {
            innerSyllableRenderer.sortingLayerName = balloonBodyRenderer.sortingLayerName;
            innerSyllableRenderer.sortingOrder = balloonBodyRenderer.sortingOrder + 10;
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.up * upSpeed * Time.deltaTime);
        if (Camera.main != null && transform.position.y > Camera.main.orthographicSize + 2f)
            Destroy(gameObject);
    }

    private void UpdateInnerSprite()
    {
        Sprite s = null;
        if (syllableStepSprites != null && syllableStepSprites.Length > 0)
        {
            int idx = Mathf.Clamp(currentStep, 0, syllableStepSprites.Length - 1);
            s = syllableStepSprites[idx];
        }

        if (innerSyllableRenderer != null) innerSyllableRenderer.sprite = s;
        else if (innerSyllableImageUI != null) innerSyllableImageUI.sprite = s;
    }

    private void OnMouseDown()
    {
        HandleClick();
    }

    public void HandleClick()
    {
        if (isPopping) return;

        PlaySyllableSound();
        currentStep++;
        
        if (syllableStepSprites != null && currentStep < syllableStepSprites.Length)
        {
            UpdateInnerSprite();
            return;
        }

        if (balloonPopSteps != null && balloonBodyRenderer != null && currentStep < balloonPopSteps.Length + syllableStepSprites.Length)
        {
            int popStepIndex = currentStep - syllableStepSprites.Length;
            if (popStepIndex >= 0 && popStepIndex < balloonPopSteps.Length)
            {
                balloonBodyRenderer.sprite = balloonPopSteps[popStepIndex];
                return;
            }
        }

        // Se passou de todas as etapas: pop final
        // Passamos a posição do OBJETO (transform.position) e não do clique
        StartCoroutine(PopSequence(transform.position));
    }

    void PlaySyllableSound()
    {
        var mm = MainGameManager.Instance;
        
        if (syllableClickSounds != null && currentStep < syllableClickSounds.Length && syllableClickSounds[currentStep] != null)
        {
            if (mm != null && mm.syllableSource != null) mm.syllableSource.PlayOneShot(syllableClickSounds[currentStep]);
            else AudioSource.PlayClipAtPoint(syllableClickSounds[currentStep], Camera.main.transform.position);
            return;
        }

        if (mm != null && mm.syllables != null && mm.currentSyllableIndex < mm.syllables.Count)
        {
            var currentSyllable = mm.syllables[mm.currentSyllableIndex];
            if (currentSyllable.syllableClip != null && mm.syllableSource != null)
                mm.syllableSource.PlayOneShot(currentSyllable.syllableClip);
        }
    }

    IEnumerator PopSequence(Vector2 popPosition)
    {
        isPopping = true;

        if (popSound != null)
        {
            var mm = MainGameManager.Instance;
            if (mm != null && mm.sfxSource != null) mm.sfxSource.PlayOneShot(popSound);
            else AudioSource.PlayClipAtPoint(popSound, Camera.main.transform.position);
        }

        if (popAnimationFrames != null && popAnimationFrames.Length > 0 && balloonBodyRenderer != null)
        {
            foreach (var f in popAnimationFrames)
            {
                balloonBodyRenderer.sprite = f;
                // Esconde a sílaba interna durante a animação de explosão para parecer que ela saiu voando
                if (innerSyllableRenderer) innerSyllableRenderer.enabled = false;
                yield return new WaitForSeconds(popFrameRate);
            }
        }

        // NOTIFICA COM POSIÇÃO
        // Importante: popPosition aqui é o transform.position que passamos no HandleClick
        onBalloonPoppedWithPosition?.Invoke(popPosition);
        onFinalPop?.Invoke();

        Destroy(gameObject);
    }
}