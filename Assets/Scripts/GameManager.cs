using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
    [Header("Sprite Manager")]
    public Sprite[] sprites;
    private int currentSpriteIndex = 0;
    private int spriteTouchCount = 0;

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

    public Sprite GetCurrentSprite()
    {
        if (sprites.Length == 0) return null;
        return sprites[currentSpriteIndex];
    }
    public void RegisterTouch(int amount)
    {
        spriteTouchCount += amount;

        if (spriteTouchCount < 0) spriteTouchCount = 0;

        if (spriteTouchCount >= 5)
        {
            spriteTouchCount = 0;
            currentSpriteIndex++;
            if (currentSpriteIndex >= sprites.Length) currentSpriteIndex = 0;
        }
    }

    public void ImageTouch()
    {
        RegisterTouch(1);
    }

    public void BombTouch()
    {
        RegisterTouch(-1);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0; // Prevent negative score
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

