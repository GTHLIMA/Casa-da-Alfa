using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScenes : MonoBehaviour
{
    public void MainMenu() => SceneManager.LoadScene("MainMenu");
    public void LoadLevels() => SceneManager.LoadScene("Levels");
    public void Level1_1() => SceneManager.LoadScene("Game1");
    public void Level1_2() => SceneManager.LoadScene("Game1.2");
    public void Level1_3() => SceneManager.LoadScene("Game1.3");



}
