using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitButton : MonoBehaviour
{
    public GameObject waitButton;
    public float waitTime = 5f; 
    void Start()
    {
        waitButton.SetActive(false); 
        StartCoroutine(WaitAndActivateButton());
        
    }

    private IEnumerator WaitAndActivateButton()
    {
        yield return new WaitForSeconds(waitTime); 
        waitButton.SetActive(true); 
    }



}
