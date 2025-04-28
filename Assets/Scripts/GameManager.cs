using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
    public GameObject[] spawnnablePrefabs;
    public float maxX;
    public Transform spawnPoint;
    public float spawnRate;
    bool gameStarted = false;
    public static GameManager Instance;
    [SerializeField] private NumberCounter numberCounter;
    private int score = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    public void AddScore(int amount)
    {
        score += amount;
        numberCounter.Value = score;
    }
    public int GetScore() => score;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !gameStarted)
        {
            StartSpawning();
            gameStarted = true;
        }
        
    }

    private void StartSpawning()
    {
        InvokeRepeating("SpawnPrefab", 0.5f, spawnRate);
    }

        private void SpawnPrefab()
    {
        if (spawnnablePrefabs.Length == 0) return;

        GameObject prefabtoSpawn = spawnnablePrefabs[Random.Range(0, spawnnablePrefabs.Length)];

        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = Random.Range(-maxX, maxX);

        Instantiate(prefabtoSpawn, spawnPosition, Quaternion.identity);

    }
}

