using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LandscapePlaceholder : MonoBehaviour
{
    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToPortrait = false;

        StartCoroutine(LoadGame2NextFrame());
    }

    private IEnumerator LoadGame2NextFrame()
    {
        yield return null;
        SceneManager.LoadScene("Game2");
    }
}
