using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnswerSlotController : MonoBehaviour
{
    [SerializeField] private Image slotImage;
    private SyllablePuzzleManager manager;

    private void Awake()
    {
        if (slotImage == null) slotImage = GetComponent<Image>();
    }

    public void Setup(SyllablePuzzleManager managerRef, Sprite questionSprite)
    {
        manager = managerRef;
        slotImage.sprite = questionSprite;
    }

    public void RevealSyllable(Sprite syllableSprite, AudioClip syllableAudio)
{
    if (manager != null)
    {
        manager.PlayAudio(syllableAudio);
        slotImage.sprite = syllableSprite;
        StartCoroutine(manager.BounceAnimation(GetComponent<RectTransform>()));

       
        gameObject.AddComponent<SlotFloatEffect>();
        }
    }
}