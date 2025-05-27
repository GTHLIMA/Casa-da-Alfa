using TMPro;
using UnityEngine;

public class FinalScoreDisplay : MonoBehaviour
{
    public TMP_Text scoreText;

    void Start()
    {
        if (ScoreTransfer.Instance != null)
        {
            scoreText.text = "Final Score: " + ScoreTransfer.Instance.Score.ToString();
        }
        else
        {
            scoreText.text = "Score not available.";
        }
    }
}
