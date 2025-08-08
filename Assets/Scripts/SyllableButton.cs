using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SyllableButton : MonoBehaviour
{
    private SyllableBuilderManager manager;
    private SyllableBuilderManager.SyllableButtonData myData;
    private Button button;

    // Método para configurar o botão com seus dados e a referência do manager
    public void Setup(SyllableBuilderManager.SyllableButtonData data, SyllableBuilderManager managerRef)
    {
        myData = data;
        manager = managerRef;

        button = GetComponent<Button>();
        // Remove listeners antigos para evitar chamadas duplicadas
        button.onClick.RemoveAllListeners(); 
        // Adiciona o listener para o método OnClick
        button.onClick.AddListener(OnClick);
    }

    // Quando o botão é clicado, ele avisa o manager
    private void OnClick()
    {
        if (manager != null)
        {
            manager.OnSyllableClicked(myData, this);
        }
    }
}