using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerName : MonoBehaviour
{
    public TMP_InputField inputName;
    public TextMeshProUGUI NameText;

    void Start()
    {
        NameText.text = "";


    }

    public void SetName()
    {
        string PlayerName = inputName.text;

        NameText.text = PlayerName;

        // Salva na RAM para outra cena
        GlobalData.playerName = PlayerName;
    }
}
