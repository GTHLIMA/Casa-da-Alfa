using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToggleFunctionality : MonoBehaviour
{

    [SerializeField] private TMP_Text textField;
    [SerializeField] private string message; 

    public void SendMessage(bool toogleValue)
    {
        if (toogleValue)
        {
            textField.SetText(message);
        }
        else
        {
            textField.SetText(sourceText: "Nothing Set!");
        }
    }

}
