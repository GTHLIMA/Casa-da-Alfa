using System;
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

    public GameObject[] spawnnablePrefabs;
    public Transform spawnPoint;
    public float horizontalSpawnPadding = 1f;
    private float maxVisibleX;
    public float spawnRate;
    bool gameStarted = false;
    public static GameManager Instance;
    [SerializeField] private NumberCounter numberCounter;
    private int score = 0;

    private bool isSpeedUp = false;
    private int speedLevel = 1;
    public float normalGravityScale = 1f;
    public float mediumGravityScale = 2f;
    public float fastUpGravityScale = 3f;
    private AudioManager audioManager;

    [Header("------------- Audio Clip -------------")]
    public AudioClip[] spriteAudios;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        maxVisibleX = Camera.main.orthographicSize * Camera.main.aspect;
    }

    public Sprite GetCurrentSprite()
    {
        if (sprites.Length == 0) return null;
        return sprites[currentSpriteIndex];
    }

    public AudioClip GetCurrentSpriteAudio()
    {
        if (spriteAudios != null && currentSpriteIndex < spriteAudios.Length)
            return spriteAudios[currentSpriteIndex];
        return null;
    }

    public void ImageTouch()
    {
        currentSpriteIndex++;
        if (currentSpriteIndex >= sprites.Length) currentSpriteIndex = 0;
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0) score = 0;
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

        GameObject prefabtoSpawn = spawnnablePrefabs[UnityEngine.Random.Range(0, spawnnablePrefabs.Length)];

        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = UnityEngine.Random.Range(-maxVisibleX + horizontalSpawnPadding, maxVisibleX - horizontalSpawnPadding);


        GameObject instance = Instantiate(prefabtoSpawn, spawnPosition, Quaternion.identity);

        Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            float gravityToApply = speedLevel == 1 ? normalGravityScale :
                                   speedLevel == 2 ? mediumGravityScale : fastUpGravityScale;

            rb.gravityScale = gravityToApply;
        }
    }

    public void ToggleSpeedUp()
    {
        isSpeedUp = !isSpeedUp;
        speedLevel++;

        if (speedLevel > 3) speedLevel = 1;

        float newGravityScale = normalGravityScale;
        float newPitch = audioManager.normalPitch;

        switch (speedLevel)
        {
            case 1:
                newGravityScale = normalGravityScale;
                newPitch = audioManager.normalPitch;
                break;
            case 2:
                newGravityScale = mediumGravityScale;
                newPitch = Mathf.Lerp(audioManager.normalPitch, audioManager.speedUpPitch, 0.5f);
                break;
            case 3:
                newGravityScale = fastUpGravityScale;
                newPitch = audioManager.speedUpPitch;
                break;
        }

        if (audioManager != null) audioManager.SetPitch(newPitch);

        foreach (var rb in FindObjectsOfType<Rigidbody2D>())
        {
            rb.gravityScale = newGravityScale;
        }
    }
}
