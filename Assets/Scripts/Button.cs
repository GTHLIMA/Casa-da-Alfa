using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonBehavior : MonoBehaviour
{
    public void LoadSettings()
    {
        SceneManager.LoadScene("Settings");
        Debug.Log("Button clicked! Loading next scene...");
    }

    public void LoadLevels()
    {
        SceneManager.LoadScene("Levels");
        Debug.Log("Button clicked! Loading next scene...");
    }

    public void LoadStart()
    {
        SceneManager.LoadScene("Start");
        Debug.Log("Button clicked! Loading next scene...");
    }

}
