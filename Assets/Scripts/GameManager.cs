using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject[] spawnnablePrefabs;
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
        if (spawnnablePrefabs.Length == 0) return;

        GameObject prefabtoSpawn = spawnnablePrefabs[Random.Range(0, spawnnablePrefabs.Length)];

        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.x = Random.Range(-maxX, maxX);

        Instantiate(prefabtoSpawn, spawnPosition, Quaternion.identity);

    }
}

