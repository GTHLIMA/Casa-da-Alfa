using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class TrainAnimator : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("O componente 'Image' no trem que vai mostrar a figura da palavra.")]
    public Image imageContainer;

    [Header("Áudios do Trem")]
    public AudioClip trainEnteringSound;
    public AudioClip trainExitingSound;

    private Animator animator;
    private AudioSource audioSource;

    private const string ANIM_TRIGGER_ENTER = "AnimateIn";
    private const string ANIM_TRIGGER_EXIT = "AnimateOut";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Anima o trem entrando na tela JÁ COM a imagem correta.
    /// </summary>
    public IEnumerator AnimateIn(Sprite spriteToShow)
    {
        // 1. Define a imagem correta ANTES de o trem se mover.
        if (imageContainer != null)
        {
            imageContainer.sprite = spriteToShow;
            imageContainer.enabled = true;
        }

        // 2. Toca o som e a animação de entrada.
        if (trainEnteringSound != null)
        {
            audioSource.PlayOneShot(trainEnteringSound);
        }
        animator.SetTrigger(ANIM_TRIGGER_ENTER);

        // 3. Espera a animação de entrada terminar.
        float entryAnimationDuration = 2.0f; // Ajuste este tempo para a sua animação!
        yield return new WaitForSeconds(entryAnimationDuration);
        Debug.Log("[TrainAnimator] - Animação de ENTRADA completa.");
    }

    /// <summary>
    /// Anima o trem saindo da tela.
    /// </summary>
    public IEnumerator AnimateOut()
    {
        if (trainExitingSound != null)
        {
            audioSource.PlayOneShot(trainExitingSound);
        }
        animator.SetTrigger(ANIM_TRIGGER_EXIT);

        float exitAnimationDuration = 2.0f; // Ajuste este tempo para a sua animação!
        yield return new WaitForSeconds(exitAnimationDuration);

        if (imageContainer != null)
        {
            imageContainer.enabled = false;
        }
        Debug.Log("[TrainAnimator] - Animação de SAÍDA completa.");
    }
}