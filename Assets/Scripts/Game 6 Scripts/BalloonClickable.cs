using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class BalloonClickable : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("SpriteRenderer do BALÃO INTEIRO (corpo colorido - NÃO será alterado, mantém animação)")]
    public SpriteRenderer balloonBodyRenderer;
    
    [Tooltip("SpriteRenderer da SÍLABA no centro (ex: 'BA', 'CA')")]
    public SpriteRenderer innerSyllableRenderer;
    
    [Tooltip("Alternativa UI: Image da sílaba (se usar Canvas)")]
    public Image innerSyllableImageUI;

    [Header("NÃO MEXER - Sprites de Estouro")]
    [Tooltip("Sprites das etapas de ESTOURO do BALÃO (mantidos do prefab original)")]
    public Sprite[] balloonPopSteps;
    
    [Header("Syllable steps (0 = initial syllable)")]
    [Tooltip("Sprites das etapas da sílaba (primeiro será substituído pela sílaba do balão)")]
    public Sprite[] syllableStepSprites;
    
    [Header("ÁUDIO - Configure aqui os sons do balão")]
    [Tooltip("Som tocado quando o balão estoura (POP)")]
    public AudioClip popSound;
    
    [Tooltip("Sons tocados em cada clique (se vazio, usa o som da sílaba do MainGameManager)")]
    public AudioClip[] syllableClickSounds;

    [Header("Pop Animation")]
    [Tooltip("Frames da animação de estouro (aplicados no CORPO do balão)")]
    public Sprite[] popAnimationFrames;
    public float popFrameRate = 0.06f;
    public float upSpeed = 1.0f;

    [HideInInspector] public int currentStep = 0;
    public event Action onFinalPop;
    public event Action<Vector2> onBalloonPoppedWithPosition;

    private bool isPopping = false;
    
    // DADOS DA SÍLABA ATUAL
    private SyllableDado currentSyllableData;

    // NOVO MÉTODO PÚBLICO: Recebe todos os dados da sílaba
    public void SetSyllableData(SyllableDado syllableData)
    {
        currentSyllableData = syllableData;
        
        // Configura APENAS o sprite da sílaba no centro do balão
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

        // NÃO alteramos o sprite do corpo do balão para manter a animação de estouro
        // A cor/variação do balão é definida pelo PREFAB que foi instanciado

        UpdateInnerSprite();
        
        // GARANTE que a sílaba fica na frente depois de configurar
        EnsureSyllableInFront();
    }

    private void Start()
    {
        UpdateInnerSprite();
        
        // GARANTIR que sílaba fica NA FRENTE do balão
        EnsureSyllableInFront();
    }
    
    // MÉTODO para garantir que a sílaba sempre fica na frente
    private void EnsureSyllableInFront()
    {
        if (innerSyllableRenderer != null && balloonBodyRenderer != null)
        {
            // Garante que a sílaba tem sorting order maior
            innerSyllableRenderer.sortingLayerName = balloonBodyRenderer.sortingLayerName;
            innerSyllableRenderer.sortingOrder = balloonBodyRenderer.sortingOrder + 10;
            
            Debug.Log($"[BalloonClickable] ✅ Sílaba sorting: Layer='{innerSyllableRenderer.sortingLayerName}' Order={innerSyllableRenderer.sortingOrder}, Balão Order={balloonBodyRenderer.sortingOrder}");
        }
        else
        {
            Debug.LogWarning("[BalloonClickable] ⚠️ Não foi possível configurar sorting - verifique se balloonBodyRenderer e innerSyllableRenderer estão atribuídos!");
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

    private void OnMouseDown()
    {
        HandleClick();
    }

    public void HandleClick()
    {
        if (isPopping) return;

        // CAPTURA POSIÇÃO DO TOQUE
        Vector2 touchPosition = GetTouchPosition();
        Debug.Log($"🎯 Balão clicado na posição: {touchPosition}");

        // TOCAR SOM DA SÍLABA ao clicar
        PlaySyllableSound();

        currentStep++;
        
        // ATUALIZA SPRITE DA SÍLABA (não do balão)
        if (syllableStepSprites != null && currentStep < syllableStepSprites.Length)
        {
            UpdateInnerSprite();
            return;
        }

        // ATUALIZA SPRITE DO CORPO DO BALÃO (animação de estouro)
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
        StartCoroutine(PopSequence(touchPosition));
    }

    // MÉTODO: Captura posição do toque
    private Vector2 GetTouchPosition()
    {
        #if UNITY_EDITOR
        return Input.mousePosition;
        #else
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;
        else
            return Vector2.zero;
        #endif
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

    // ANIMAÇÃO DE POP
    IEnumerator PopSequence(Vector2 touchPosition)
    {
        isPopping = true;

        // TOCAR SOM DE ESTOURO (POP)
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

        // Animação de frames (aplicada no CORPO do balão)
        if (popAnimationFrames != null && popAnimationFrames.Length > 0 && balloonBodyRenderer != null)
        {
            foreach (var f in popAnimationFrames)
            {
                balloonBodyRenderer.sprite = f;
                yield return new WaitForSeconds(popFrameRate);
            }
        }

        // NOTIFICA COM POSIÇÃO
        onBalloonPoppedWithPosition?.Invoke(touchPosition);
        
        // Notifica o manager
        onFinalPop?.Invoke();

        Destroy(gameObject);
    }
}