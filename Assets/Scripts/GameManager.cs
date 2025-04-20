using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject bomb;
    public float maxX;
    public Transform spawnPoint;
    public float spawnRate;
    bool gameStarted = false;
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
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = Random.Range(-maxX, maxX);

        Instantiate(bomb, spawnPosition, Quaternion.identity);

    }
}

