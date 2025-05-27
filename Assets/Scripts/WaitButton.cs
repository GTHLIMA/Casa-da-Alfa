using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WaitButton : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject playButton;
    public GameObject slingshot;


    AudioManager audioManager;
    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    public void hidePlayButton()
    {
        playButton.SetActive(false);
        audioManager.PlayAudio(audioManager.background);
        GameObject handDrag = GameObject.FindGameObjectWithTag("teste");
        if (handDrag != null) Destroy(handDrag);


    }
    public void hidePlayButton1()
    {
        playButton.SetActive(false);
        slingshot.SetActive(true);
        audioManager.PlayAudio(audioManager.background);
        GameObject handDrag = GameObject.FindGameObjectWithTag("teste");
        if (handDrag != null) Destroy(handDrag);
    }



}
