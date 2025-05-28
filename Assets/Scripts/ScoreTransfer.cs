using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreTransfer : MonoBehaviour
{
    public static ScoreTransfer Instance { get; private set; }
    private int score = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("ScoreTransfer ativo. Score atual: " + score);

        // Escuta quando uma nova cena é carregada
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Remove o listener para evitar erros
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            Destroy(gameObject);
            Debug.Log("ScoreTransfer destruído ao voltar para o MainMenu.");
        }
    }

    public int Score => score;

    public void SetScore(int value)
    {
        score = value;
        Debug.Log("ScoreTransfer Score agora é: " + score);
    }
}
