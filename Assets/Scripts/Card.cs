using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private AudioClip cardAudio;

    private AudioSource audioSource;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;

    public bool isSelected = true;

    [HideInInspector] public CardsController controller;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }


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

    public void SetAudio(AudioClip clip) => cardAudio = clip;

    public void PlayAudio(System.Action onFinished)
    {
        if (cardAudio != null)
        {
            audioSource.clip = cardAudio;
            audioSource.Play();
            controller.StartCoroutine(WaitForAudio(onFinished));
        }
        else
        {
            onFinished?.Invoke();
        }
    }

    private IEnumerator WaitForAudio(System.Action onFinished)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        onFinished?.Invoke();
    }
}
