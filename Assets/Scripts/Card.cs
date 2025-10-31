using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private AudioClip cardAudio;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;

    public bool isSelected = true;

    [HideInInspector] public CardsController controller;

    public void Initialize(Sprite sp, AudioClip clip, CardsController ctrl)
    {
        iconSprite = sp;
        cardAudio = clip;
        controller = ctrl;

        if (iconImage != null && hiddenIconSprite != null)
            iconImage.sprite = hiddenIconSprite;

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
        Tween.Rotation(transform, new Vector3(0f, 180f, 0f), 0.2f);
        Tween.Delay(0.1f, () => { if (iconImage != null) iconImage.sprite = iconSprite; });
        isSelected = false;
    }

    public void Hide()
    {
        Tween.Rotation(transform, new Vector3(0f, 0f, 0f), 0.2f);
        Tween.Delay(0.1f, () =>
        {
            if (iconImage != null && hiddenIconSprite != null) iconImage.sprite = hiddenIconSprite;
            isSelected = true;
        });
    }

    /// <summary>
    /// Anima a carta quando é acertada: sobe e desaparece.
    /// </summary>
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

    /// <summary>
    /// Toca o áudio da carta usando o sistema de áudio do controller (syllableSource)
    /// </summary>
    public void PlayAudio(System.Action onFinished)
    {
        if (controller != null && cardAudio != null)
        {
            // Usa o novo sistema de áudio centralizado do controller
            controller.PlaySyllable(cardAudio, onFinished);
        }
        else
        {
            onFinished?.Invoke();
        }
    }
}