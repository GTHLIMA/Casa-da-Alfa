using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadScenes : MonoBehaviour {
    public static void LoadSceneByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Índice {index} inválido. Total de cenas: {SceneManager.sceneCountInBuildSettings}");
            return;
        }
        
        SceneManager.LoadScene(index);
    }
}