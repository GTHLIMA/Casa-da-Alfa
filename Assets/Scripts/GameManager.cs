using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    #region Balloon Prefabs

    [Header("======= Ballon Prefabs =======")]
    public GameObject[] spawnnablePrefabs; // Array de prefabs que podem ser spawnados
    public float horizontalSpawnPadding = 1f; // Margem horizontal para spawn para não nascer nas bordas
    public static int CurrentDropIndex = 0; 
    public static GameManager Instance; // Instância singleton do GameManager
    public Transform spawnPoint; 
    private float maxVisibleX; 
    public float spawnRate;

    #endregion



    #region Score Settings

    [Header("======= Score Settings =======")]
    [SerializeField] private NumberCounter numberCounter; // Referência para o contador de números
    private int score; // Pontuação atual do jogador
    public TMP_Text scorePause; // Texto para mostrar a pontuação no menu de pausa
    public TMP_Text scoreEndPhase; // Texto para mostrar a pontuação no fim da fase

    #endregion


    #region Game Settings

    [Header("======= Game Settings =======")]
    [SerializeField] private GameObject endPhasePanel; 
    public static bool GameStarted = false; 
    private bool gameStarted = false; 
    public GameObject PauseMenu;
    private AudioManager audioManager; // Referência para o gerenciador de áudio

    #endregion


    private void Awake()
    {
        Time.timeScale = 1f; 
        
        // Configura o padrão singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);


        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        
        // Calcula o limite máximo visível no eixo X baseado na câmera
        maxVisibleX = Camera.main.orthographicSize * Camera.main.aspect;
    }

    private void Start()
    {
        // Inicializa a pontuação com o valor transferido de outra cena
        score = ScoreTransfer.Instance.Score;
        numberCounter.Value = score;
    }

    private void Update()
    {
        // Verifica se o jogador clicou na tela para começar o jogo
        if (Input.GetMouseButtonDown(0) && !gameStarted)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (gameStarted) return;

        CurrentDropIndex = 0; // Reseta o índice de drops no inicio do jogo

        StartSpawning();
        gameStarted = true;
        GameStarted = true;

        FindObjectOfType<AudioManager>().SetBackgroundVolume(0.05f);
        Array.ForEach(GameObject.FindGameObjectsWithTag("teste"), Destroy);
    }



    private void StartSpawning()
    {
        // Velocidade do spawn dos prefabs
        InvokeRepeating(nameof(SpawnPrefab), 0.5f, spawnRate);
    }

    private void SpawnPrefab()
    {
        // Sai do método se não houver prefabs configurados
        if (spawnnablePrefabs.Length == 0) return;

        // Escolhe um prefab aleatório do array
        GameObject prefabToSpawn = spawnnablePrefabs[UnityEngine.Random.Range(0, spawnnablePrefabs.Length)];

        // Calcula uma posição de spawn aleatória dentro dos limites da tela
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = UnityEngine.Random.Range(-maxVisibleX + horizontalSpawnPadding, maxVisibleX - horizontalSpawnPadding);

        // Instancia o prefab na posição calculada
        GameObject instance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }

    public void CheckEndPhase(int currentIndex, int totalDrops)
    {
        // Se o índice atual for maior ou igual ao total, mostra o painel de fim de fase
        if (currentIndex >= totalDrops)
        {
            ShowEndPhasePanel();
        }
    }

    public void OpenPauseMenuLvl1()
    {
        // Atualiza o texto da pontuação no menu de pausa
        if (scorePause != null) scorePause.text = "Score: " + score.ToString();
        
        PauseMenu.SetActive(true); 
        audioManager.PauseAudio(audioManager.background); 
        Time.timeScale = 0; 
        ScoreTransfer.Instance.SetScore(score); // Salva a pontuação atual
    }

    public void ClosePauseMenuLvl1()
    {
        Time.timeScale = 1f; 
        PauseMenu.SetActive(false); 
        audioManager.ResumeAudio(audioManager.background); 
    }

    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        yield return new WaitForSeconds(0.5f); 

        // Atualiza o texto da pontuação no painel de fim de fase
        if (scoreEndPhase != null)
            scoreEndPhase.text = "Score: " + score.ToString();

        Time.timeScale = 0f; 
        audioManager.PauseAudio(audioManager.background); 
        endPhasePanel.SetActive(true); 

        spawnPoint.gameObject.SetActive(false); // Desativa o ponto de spawn

        ScoreTransfer.Instance.SetScore(score); // Salva a pontuação
        audioManager.PlaySFX(audioManager.end2); 
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0; // Garante que a pontuação não fique negativa

        numberCounter.Value = score; // Atualiza o contador visual
        ScoreTransfer.Instance.SetScore(score); // Salva a pontuação
    }
    
    // Retorna a pontuação atual
    public int GetScore() => score;
}