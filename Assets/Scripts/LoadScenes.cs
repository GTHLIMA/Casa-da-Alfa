using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScenes : MonoBehaviour
{
    public void MainMenu() => LoadSceneWithOrientation("MainMenu", false);
    public void Level1() => LoadSceneWithOrientation("Game1", false);
    public void Level1_3() => LoadSceneWithOrientation("Game1.3", false);
    public void Level2() => LoadSceneWithOrientation("Game2", false);
    public void Level2_1() => LoadSceneWithOrientation("Game2.1", false);
    public void Level2_2() => LoadSceneWithOrientation("Game2.2", false);
    public void Level2_3() => LoadSceneWithOrientation("Game2.3", false);
    public void Level2_4() => LoadSceneWithOrientation("Game2.4", false);
    public void Level2_5() => LoadSceneWithOrientation("Game2.5", false);
    public void Level3() => LoadSceneWithOrientation("Game3", false);
    public void Level3_1() => LoadSceneWithOrientation("Game3.1", false);

    private void LoadSceneWithOrientation(string sceneName, bool isLandscape)
    {
        // Definir orientação antes de carregar
        if (isLandscape)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = false;
        }

        else
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = false;
        }

        // Carregar a cena e forçar update da UI
        StartCoroutine(LoadAndFixUI(sceneName));
    }

    private IEnumerator LoadAndFixUI(string sceneName)
    {
        SceneManager.LoadScene(sceneName);

        yield return null;
        Canvas.ForceUpdateCanvases();

        yield return null;
        Canvas.ForceUpdateCanvases();
    }
}
