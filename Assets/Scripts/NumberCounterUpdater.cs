using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NumberCounterUpdater : MonoBehaviour
{
    public NumberCounter numberCounter; // Reference to the NumberCounter script
    public TMP_InputField InputField; // The target value to count to

    private void SetValue()
    {
        int value;

        if (int.TryParse(InputField.text, out value))
        {
            numberCounter.Value = value; // Set the value in the NumberCounter script
        }
    }
}
