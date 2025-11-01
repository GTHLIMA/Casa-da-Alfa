using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class BalloonClickable : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer balloonSpriteRenderer;   // sprite do balão (root child)
    public SpriteRenderer innerSyllableRenderer;   // sprite no centro (SpriteRenderer)
    public Image innerSyllableImageUI;             // alternativa: UI Image (opcional)

    [Header("Syllable steps (0 = initial syllable)")]
    public Sprite[] syllableStepSprites;           // primeiro elemento será substituído com a sílaba atual
    
    [Header("🔊 ÁUDIO - Configure aqui os sons do balão")]
    [Tooltip("Som tocado quando o balão estoura (POP)")]
    public AudioClip popSound;                     // ← SOM DE ESTOURO DO BALÃO
    
    [Tooltip("Sons tocados em cada clique (se vazio, usa o som da sílaba do MainGameManager)")]
    public AudioClip[] syllableClickSounds;        // ← SONS DAS SÍLABAS POR CLIQUE (opcional)

    [Header("Pop Animation")]
    public Sprite[] popAnimationFrames;
    public float popFrameRate = 0.06f;
    public float upSpeed = 1.0f;                   // velocidade de subida (units/sec)

    [HideInInspector] public int currentStep = 0;
    public event Action onFinalPop;                 // notifica o BalloonManager/MainGameManager

    private bool isPopping = false;

    // --- API pública chamada pelo BalloonManager ao instanciar ---
    public void SetSyllableSprite(Sprite syllableSprite)
    {
        if (syllableStepSprites == null || syllableStepSprites.Length == 0)
        {
            syllableStepSprites = new Sprite[] { syllableSprite };
            currentStep = 0;
        }
        else
        {
            syllableStepSprites[0] = syllableSprite;
            currentStep = 0;
        }

        UpdateInnerSprite();
    }

    private void Start()
    {
        UpdateInnerSprite();
        
        // 🆕 GARANTIR que sílaba fica NA FRENTE do balão
        if (innerSyllableRenderer != null && balloonSpriteRenderer != null)
        {
            // Sílaba tem sorting order maior = aparece na frente
            innerSyllableRenderer.sortingOrder = balloonSpriteRenderer.sortingOrder + 1;
            Debug.Log($"[BalloonClickable] Sílaba sortingOrder: {innerSyllableRenderer.sortingOrder}, Balão: {balloonSpriteRenderer.sortingOrder}");
        }
    }

    private void Update()
    {
        // mover para cima
        transform.Translate(Vector3.up * upSpeed * Time.deltaTime);

        // auto-destroy se sair da tela
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

        if (innerSyllableRenderer != null)
            innerSyllableRenderer.sprite = s;
        else if (innerSyllableImageUI != null)
            innerSyllableImageUI.sprite = s;
    }

    // Touch / click handling
    private void OnMouseDown()
    {
        HandleClick();
    }

    public void HandleClick()
    {
        if (isPopping) return;

        // 🔊 TOCAR SOM DA SÍLABA ao clicar
        PlaySyllableSound();

        currentStep++;
        if (syllableStepSprites != null && currentStep < syllableStepSprites.Length)
        {
            UpdateInnerSprite();
            return;
        }

        // Se passou do último passo: pop
        StartCoroutine(PopSequence());
    }

    void PlaySyllableSound()
    {
        var mm = MainGameManager.Instance;
        
        // Prioridade 1: Som específico do array syllableClickSounds
        if (syllableClickSounds != null && currentStep < syllableClickSounds.Length && syllableClickSounds[currentStep] != null)
        {
            if (mm != null && mm.syllableSource != null)
                mm.syllableSource.PlayOneShot(syllableClickSounds[currentStep]);
            else
                AudioSource.PlayClipAtPoint(syllableClickSounds[currentStep], Camera.main.transform.position);
            
            return;
        }

        // Prioridade 2: Som da sílaba atual do MainGameManager
        if (mm != null && mm.syllables != null && mm.currentSyllableIndex < mm.syllables.Count)
        {
            var currentSyllable = mm.syllables[mm.currentSyllableIndex];
            if (currentSyllable.syllableClip != null && mm.syllableSource != null)
            {
                mm.syllableSource.PlayOneShot(currentSyllable.syllableClip);
            }
        }
    }

    IEnumerator PopSequence()
    {
        isPopping = true;

        // 🔊 TOCAR SOM DE ESTOURO (POP)
        if (popSound != null)
        {
            var mm = MainGameManager.Instance;
            if (mm != null && mm.sfxSource != null)
            {
                mm.sfxSource.PlayOneShot(popSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(popSound, Camera.main.transform.position);
            }
        }

        // Animação de frames (se existirem)
        if (popAnimationFrames != null && popAnimationFrames.Length > 0 && balloonSpriteRenderer != null)
        {
            foreach (var f in popAnimationFrames)
            {
                balloonSpriteRenderer.sprite = f;
                yield return new WaitForSeconds(popFrameRate);
            }
        }

        // Notifica o manager (arc++ será feito por quem escuta esse evento)
        onFinalPop?.Invoke();

        Destroy(gameObject);
    }
}