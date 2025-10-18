using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class BalloonClickable : MonoBehaviour
{
    [Header("Renderers (select one)")]
    public SpriteRenderer balloonSpriteRenderer;   // sprite do balão (root child)
    public SpriteRenderer innerSyllableRenderer;   // sprite no centro (SpriteRenderer)
    public Image innerSyllableImageUI;             // alternativa: UI Image (opcional)

    [Header("Syllable steps (0 = initial syllable)")]
    public Sprite[] syllableStepSprites;           // primeiro elemento será substituído com a sílaba atual
    public AudioClip[] syllableClips;              // opcional: sons por etapa

    [Header("Pop/Movement")]
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
            // Garante pelo menos 1 slot
            syllableStepSprites = new Sprite[] { syllableSprite };
            currentStep = 0;
        }
        else
        {
            // sobrescreve o primeiro sprite com a sílaba atual
            syllableStepSprites[0] = syllableSprite;
            currentStep = 0;
        }

        UpdateInnerSprite();
    }

    private void Start()
    {
        UpdateInnerSprite();
    }

    private void Update()
    {
        // mover para cima (simples)
        transform.Translate(Vector3.up * upSpeed * Time.deltaTime);

        // auto-destroy se sair da tela (ajuste Y limite conforme camera)
        if (transform.position.y > Camera.main.orthographicSize + 2f)
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

    // Touch / click handling: funciona no Editor (OnMouseDown) e mobile (toca com touch raycast)
    private void OnMouseDown()
    {
        HandleClick();
    }

    public void HandleClick()
    {
        if (isPopping) return;

        // toca som da etapa (se houver)
        if (currentStep < syllableClips.Length && syllableClips[currentStep] != null)
        {
            var mm = MainGameManager.Instance;
            if (mm != null && mm.syllableSource != null)
                mm.syllableSource.PlayOneShot(syllableClips[currentStep]);
            else
                AudioSource.PlayClipAtPoint(syllableClips[currentStep], Camera.main.transform.position);
        }
        else
        {
            // alternativa: tocar o clip da sílaba via MainGameManager se disponível (primeiro)
            var mm = MainGameManager.Instance;
            if (mm != null && mm.syllableSource != null && syllableStepSprites.Length > 0)
            {
                // tenta tocar mm.syllableSource com o clip associado no MainGameManager list (opcional)
            }
        }

        currentStep++;
        if (syllableStepSprites != null && currentStep < syllableStepSprites.Length)
        {
            UpdateInnerSprite();
            return;
        }

        // se passou do último passo: pop
        StartCoroutine(PopSequence());
    }

    IEnumerator PopSequence()
    {
        isPopping = true;

        // tocar pop SFX via MainGameManager.sfxSource se disponível
        var mm = MainGameManager.Instance;
        if (mm != null && mm.sfxSource != null)
        {
            // mm.sfxSource.PlayOneShot(mm.popClip) // se você tiver popClip no manager
        }

        // animação de frames (se existirem)
        if (popAnimationFrames != null && popAnimationFrames.Length > 0 && balloonSpriteRenderer != null)
        {
            foreach (var f in popAnimationFrames)
            {
                balloonSpriteRenderer.sprite = f;
                yield return new WaitForSeconds(popFrameRate);
            }
        }

        // notifica o manager (arc++ será feito por quem escuta esse evento)
        onFinalPop?.Invoke();

        Destroy(gameObject);
    }
}
