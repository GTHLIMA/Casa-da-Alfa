using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArcProgressController : MonoBehaviour
{
    [Header("UI Elements")]
    public Image arcFillImage; 
    public Image centerSyllableImage;

    [Header("Settings")]
    public int maxSteps = 5;

    private int currentSteps = 0;

    public void SetSyllable(Sprite s)
    {
        if (centerSyllableImage != null) centerSyllableImage.sprite = s;
    }

    public void IncrementProgress()
    {
        currentSteps = Mathf.Clamp(currentSteps + 1, 0, maxSteps);
        UpdateUI();
    }

    public void ResetArc()
    {
        currentSteps = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (arcFillImage != null) arcFillImage.fillAmount = (float)currentSteps / (float)maxSteps;
    }

    public bool IsComplete()
    {
        return currentSteps >= maxSteps;
    }
}