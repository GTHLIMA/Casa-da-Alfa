using UnityEngine;
using UnityEngine.UI;

public class ArcProgressController : MonoBehaviour
{
    [Header("UI References")]
    public Image arcFillImage;            // imagem circular que enche
    public Image centerSyllableImage;     // imagem da sílaba

    [Header("Settings")]
    public int maxSteps = 5;              // quantos balões até completar
    private int currentSteps = 0;

    // Reseta o arco
    public void ResetArc()
    {
        currentSteps = 0;
        if (arcFillImage != null)
            arcFillImage.fillAmount = 0;
    }

    // Define a sílaba no centro
    public void SetSyllable(Sprite s)
    {
        if (centerSyllableImage != null)
            centerSyllableImage.sprite = s;
    }

    // Incrementa o progresso
    public void IncrementProgress()
    {
        currentSteps++;
        if (arcFillImage != null)
        {
            float fill = Mathf.Clamp01((float)currentSteps / maxSteps);
            arcFillImage.fillAmount = fill;
        }
    }

    // Verifica se completou
    public bool IsComplete()
    {
        return currentSteps >= maxSteps;
    }
}
