using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NumberCounter : MonoBehaviour
{
    public TextMeshProUGUI Text; 
    public string NumberFormat = "D3";
    public int CountFPS = 30;
    public float Duration = 1f;
    private int _value;

    public int Value
{
    get { return _value; }
    set 
    {
        int clampedValue = Mathf.Clamp(value, 0, 999); // 👈 limita entre 0 e 999
        UpadteText(clampedValue);
        _value = clampedValue;
    } 
}


    private Coroutine CountingCoroutine;

    private void Awake()
    {
        Text  = GetComponent<TextMeshProUGUI>();
        
    }

    private void UpadteText(int newValue)
    {
        if (CountingCoroutine != null)
        {
            StopCoroutine(CountingCoroutine);
        }

        CountingCoroutine = StartCoroutine(CountText(newValue));
    }

    private IEnumerator CountText(int newValue)
    {
        WaitForSeconds wait = new WaitForSeconds(1f / CountFPS);
        int previousValue = _value;
        int stepAmount;

        if (newValue - previousValue < 0)
        {
            stepAmount = Mathf.FloorToInt((newValue - previousValue) / (Duration * CountFPS));
        }
        else 
        {
            stepAmount = Mathf.CeilToInt((newValue - previousValue) / (Duration * CountFPS));
        }

        if (previousValue < newValue)
        {
            while (previousValue < newValue)
            {
                previousValue += stepAmount;
                if (previousValue > newValue)
                {
                    previousValue = newValue;
                }
                Text.SetText(previousValue.ToString(NumberFormat));

                yield return wait;
            }
        }
        else 
        {
            while (previousValue > newValue)
            {
                previousValue += stepAmount;
                if (previousValue < newValue)
                {
                    previousValue = newValue;
                }
                Text.SetText(previousValue.ToString(NumberFormat));

                yield return wait;
            }
        }

    }



}
