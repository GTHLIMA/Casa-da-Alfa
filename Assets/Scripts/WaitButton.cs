using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitButton : MonoBehaviour
{
    public GameObject waitButton;
    public GameObject playButton;

    AudioManager audioManager;
    public float waitTime = 5f; 

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }
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

    public void hidePlayButton()
    {
        playButton.SetActive(false); 
        audioManager.PlayAudio(audioManager.background);
    }



}
