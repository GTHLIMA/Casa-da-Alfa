using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class OptionButton : MonoBehaviour
{
    [Header("UI References")]
    public Image background;
    public Image icon;
    public Image feedback;      // overlay X or CHECK
    public Image syllableLabel; // image da sílaba (ex: "BA") - começa invisível

    [Header("Animation Settings")]
    public float fadeDuration = 0.25f;
    public float bounceScale = 1.2f;
    public float feedbackDuration = 1.0f;

    private Button btn;
    private Action onClickCallback;

    void Awake()
    {
        btn = GetComponent<Button>();
        ResetVisuals();
    }

    void ResetVisuals()
    {
        if (feedback != null) SetAlpha(feedback, 0f);
        if (syllableLabel != null) SetAlpha(syllableLabel, 0f);
        if (feedback != null) feedback.transform.localScale = Vector3.one;
        if (syllableLabel != null) syllableLabel.transform.localScale = Vector3.one;
    }

    void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    // Setup chamado pelo RoundController -> agora recebe a sprite da sílaba também
    public void Setup(Sprite iconSprite, Sprite syllableSprite, Action onClick)
    {
        if (icon != null) icon.sprite = iconSprite;
        if (syllableLabel != null && syllableSprite != null) syllableLabel.sprite = syllableSprite;

        onClickCallback = onClick;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => { onClickCallback?.Invoke(); });

        ResetVisuals();
        SetInteractable(true);
    }

    public void SetInteractable(bool state)
    {
        if (btn != null) btn.interactable = state;
        if (background != null)
            background.color = state ? Color.white : new Color(1f, 1f, 1f, 0.6f);
    }

    // Mostra a sílaba referente ao desenho (fade + bounce)
    public void ShowSyllable()
    {
        if (syllableLabel == null) return;

        // garante que alpha começa em 0
        syllableLabel.color = new Color(syllableLabel.color.r, syllableLabel.color.g, syllableLabel.color.b, 0f);
        syllableLabel.transform.localScale = Vector3.one * 0.9f;

        Sequence.Create()
            .Group(Tween.Alpha(syllableLabel, 1f, fadeDuration, Ease.OutQuad))
            .Group(Tween.Scale(syllableLabel.transform, bounceScale, 0.28f, Ease.OutBack))
            .Chain(Tween.Scale(syllableLabel.transform, 1f, 0.18f, Ease.OutBack));
    }

    // Mostra feedback (check ou X) com fade+bounce e fade out depois de feedbackDuration
    public void ShowFeedback(bool isCorrect, Sprite sprite)
    {
        if (feedback == null) return;
        if (sprite != null) feedback.sprite = sprite;

        feedback.color = new Color(feedback.color.r, feedback.color.g, feedback.color.b, 0f);
        feedback.transform.localScale = Vector3.one * 0.9f;

        Sequence.Create()
            .Group(Tween.Alpha(feedback, 1f, fadeDuration))
            .Group(Tween.Scale(feedback.transform, bounceScale, 0.28f, Ease.OutBack))
            .Chain(Tween.Scale(feedback.transform, 1f, 0.18f, Ease.OutBack))
            .ChainDelay(feedbackDuration)
            .Chain(Tween.Alpha(feedback, 0f, 0.28f));
    }
}
