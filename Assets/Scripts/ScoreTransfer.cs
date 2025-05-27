using UnityEngine;

public class ScoreTransfer : MonoBehaviour
{
    public static ScoreTransfer Instance { get; private set; }
    private int score = 000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Garante que apenas 1 sobrevivas
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("ScoreTransfer ativo. Score atual: " + score);
    }


    public int Score => score;

    public void SetScore(int value)
    {
        score = value;
        Debug.Log("ScoreTransfer Score agora é: " + score);
    }
}
