using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private AudioClip cardAudio;

    [Header("Sprites")]
    public Sprite hiddenIconSprite; // O verso (ButtonText...)
    public Sprite iconSprite;       // A frente (Balão...)

    [Header("Configuração de Escala")]
    [Tooltip("Tamanho da imagem quando a carta está revelada (Ex: Balão). Use valores maiores que 1 para aumentar.")]
    [SerializeField] private Vector3 faceUpScale = Vector3.one; 
    
    [Tooltip("Tamanho da imagem quando a carta está escondida (Verso). Geralmente fica em (1,1,1).")]
    [SerializeField] private Vector3 faceDownScale = Vector3.one;

    public bool isSelected = true;
    
    // Variável interna para controlar se corrigimos o espelhamento
    private bool fixMirroredContent = true;

    [HideInInspector] public CardsController controller;

    public void Initialize(Sprite sp, AudioClip clip, CardsController ctrl, bool fixMirrored = true)
    {
        iconSprite = sp;
        cardAudio = clip;
        controller = ctrl;
        this.fixMirroredContent = fixMirrored;

        if (iconImage != null && hiddenIconSprite != null)
        {
            iconImage.sprite = hiddenIconSprite;
            // Aplica a escala configurada para o VERSO
            iconImage.transform.localScale = faceDownScale; 
        }

        isSelected = true;
        transform.localEulerAngles = Vector3.zero;

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnCardClick);
        }
    }

    public void OnCardClick()
    {
        controller.SetSelected(this);
    }

    public void Show()
    {
        // Gira a carta (pai) para 180 graus
        Tween.Rotation(transform, new Vector3(0f, 180f, 0f), 0.2f);
        
        // No meio da animação (0.1s), troca o sprite e ajusta o tamanho
        Tween.Delay(0.1f, () => 
        { 
            if (iconImage != null) 
            {
                iconImage.sprite = iconSprite;
                
                // Aplica a escala configurada para a FRENTE (Balão)
                Vector3 targetScale = faceUpScale;

                // Lógica de correção de espelho:
                // Se fixMirroredContent for true, invertemos o X da escala escolhida
                if (fixMirroredContent)
                {
                    targetScale.x = -Mathf.Abs(targetScale.x); // Garante que seja negativo
                }
                
                iconImage.transform.localScale = targetScale;
            } 
        });
        
        isSelected = false;
    }

    public void Hide()
    {
        // Gira de volta para 0
        Tween.Rotation(transform, new Vector3(0f, 0f, 0f), 0.2f);
        
        // No meio da animação, troca para o verso e reseta a escala
        Tween.Delay(0.1f, () =>
        {
            if (iconImage != null && hiddenIconSprite != null) 
            {
                iconImage.sprite = hiddenIconSprite;
                
                // Aplica a escala configurada para o VERSO (ButtonText)
                // Aqui não precisa inverter X pois a rotação voltou a 0
                iconImage.transform.localScale = faceDownScale;
            }
            isSelected = true;
        });
    }

    public void CorrectMatch()
    {
        // Flutua para cima
        Tween.LocalPositionY(transform, transform.localPosition.y + 100f, 0.5f, ease: Ease.OutCubic);

        // Faz fade out em todos os elementos gráficos do prefab
        Graphic[] graphics = GetComponentsInChildren<Graphic>();
        foreach (var g in graphics)
        {
            Tween.Alpha(g, 0f, 0.5f);
        }
    }

    public void SetAudio(AudioClip clip) => cardAudio = clip;

    public void PlayAudio(System.Action onFinished)
    {
        if (controller != null && cardAudio != null)
        {
            controller.PlaySyllable(cardAudio, onFinished);
        }
        else
        {
            onFinished?.Invoke();
        }
    }
}