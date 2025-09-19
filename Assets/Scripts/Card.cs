using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class Card : MonoBehaviour
{   
    [SerializeField] private Image iconImage;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;
    public bool isSelected;
    
    public CardsController controller;


    public void OnCardClick()
    {
        controller.SetSelected(this);   
    }

    public void Show()
    {      
        Tween.Rotation(transform, new Vector3(0f, 180f, 0f), 0.2f);
        Tween.Delay(0.1f, () => iconImage.sprite = iconSprite);
        isSelected = false;
    }

    public void Hide()
    {
        Tween.Rotation(transform,
         new Vector3(0f, 0f, 0f), 0.2f);

        Tween.Delay(0.1f, () => {

        iconImage.sprite = hiddenIconSprite;
        isSelected = true;
        });
    }

    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
    }
}

