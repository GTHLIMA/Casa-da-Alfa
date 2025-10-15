using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    #region Balloon Prefabs

    [Header("======= Ballon Prefabs =======")]
    public GameObject[] spawnnablePrefabs;
    public float horizontalSpawnPadding = 1f;
    public static int CurrentDropIndex = 0;
    public static GameManager Instance;
    public Transform spawnPoint;
    private float maxVisibleX;
    public float spawnRate;

    #endregion

    #region Score Settings

    [Header("======= Score Settings =======")]
    [SerializeField] private NumberCounter numberCounter;
    private int score;
    public TMP_Text scorePause;
    public TMP_Text scoreEndPhase;

    #endregion

    #region Game Settings

    [Header("======= Game Settings =======")]
    [SerializeField] private GameObject endPhasePanel;
    public ParticleSystem confettiEffect; 
    public static bool GameStarted = false;
    private bool gameStarted = false;
    public GameObject PauseMenu;
    private AudioManager audioManager;

    #endregion

    private void Awake()
    {
        Time.timeScale = 1f;
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        maxVisibleX = Camera.main.orthographicSize * Camera.main.aspect;
    }

    private void Start()
    {
        if (ScoreTransfer.Instance != null) score = ScoreTransfer.Instance.Score;
        if (numberCounter != null) numberCounter.Value = score;
    }

    private void Update()
    {
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
        if (FindObjectOfType<AudioManager>() != null)
            FindObjectOfType<AudioManager>().SetBackgroundVolume(0.05f);
        Array.ForEach(GameObject.FindGameObjectsWithTag("teste"), Destroy);
    }

    private void StartSpawning()
    {
        InvokeRepeating(nameof(SpawnPrefab), 0.5f, spawnRate);
    }

    private void SpawnPrefab()
    {
        if (spawnnablePrefabs.Length == 0) return;
        GameObject prefabToSpawn = spawnnablePrefabs[UnityEngine.Random.Range(0, spawnnablePrefabs.Length)];
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = UnityEngine.Random.Range(-maxVisibleX + horizontalSpawnPadding, maxVisibleX - horizontalSpawnPadding);
        GameObject instance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }

    public void CheckEndPhase(int currentIndex, int totalDrops)
    {
        if (currentIndex >= totalDrops)
        {
            ShowEndPhasePanel();
        }
    }

    public void OpenPauseMenuLvl1()
{
    // Atualiza o score no painel
    if (scorePause != null)
        scorePause.text = "Score: " + score.ToString();

    // Ativa o painel de pausa
    if (PauseMenu != null)
    {
        PauseMenu.SetActive(true);

        // Garante que o painel da UI continue recebendo cliques
        CanvasGroup cg = PauseMenu.GetComponent<CanvasGroup>();
        if (cg == null) cg = PauseMenu.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    // Pausa música e áudio ambiente
    if (audioManager != null && audioManager.background != null)
        audioManager.PauseAudio(audioManager.background);

    // Pausa o tempo do jogo (animações, físicas, coroutines baseadas em deltaTime)
    Time.timeScale = 0f;

    // (Opcional) Pausa outros sistemas de som
    AudioListener.pause = true;

    // Salva score se houver sistema de transferência
    if (ScoreTransfer.Instance != null)
        ScoreTransfer.Instance.SetScore(score);

    Debug.Log("Jogo pausado: tempo parado e painel ativo.");
}
    public void ClosePauseMenuLvl1()
{
    // Retoma o tempo do jogo
    Time.timeScale = 1f;

    // Retoma todos os áudios pausados
    AudioListener.pause = false;
    if (audioManager != null && audioManager.background != null)
        audioManager.ResumeAudio(audioManager.background);

    // Desativa o painel de pausa
    if (PauseMenu != null)
        PauseMenu.SetActive(false);

    Debug.Log("Jogo retomado.");
}
    public void ShowEndPhasePanel()
    {
        StartCoroutine(ShowEndPhasePanelCoroutine());
    }

    private IEnumerator ShowEndPhasePanelCoroutine()
    {
        float totalTime = BalloonGameLogger.Instance.EndSession();
        Debug.Log($"O jogador ficou {totalTime:F2} segundos no jogo");
        yield return new WaitForSeconds(0.5f);

        if (scoreEndPhase != null)
            scoreEndPhase.text = "Score: " + score.ToString();

        if (endPhasePanel != null) endPhasePanel.SetActive(true);
        if (spawnPoint != null) spawnPoint.gameObject.SetActive(false);
        if (confettiEffect != null)
        {
            confettiEffect.Play();
            Debug.Log("Efeito de confete ativado!");
        }

        CancelInvoke(nameof(SpawnPrefab));
        Debug.Log("Spawner de balões parado via CancelInvoke.");

        if (audioManager != null)
        {
            
            audioManager.PauseAudio(audioManager.background);
            audioManager.PlaySFX(audioManager.end2);
        }

        if (ScoreTransfer.Instance != null) ScoreTransfer.Instance.SetScore(score);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
        if (numberCounter != null) numberCounter.Value = score;
        if (ScoreTransfer.Instance != null) ScoreTransfer.Instance.SetScore(score);
    }

    public int GetScore() => score;
}