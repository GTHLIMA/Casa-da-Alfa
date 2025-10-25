using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    [Header("Balloon Settings")]
    public GameObject balloonPrefab;
    public Transform spawnPointCenter;
    public float spawnRate = 1.0f;
    public float spawnXPadding = 2.0f;
    public int maxBalloons = 6;

    [HideInInspector] public System.Action onBalloonPopped;

    private bool spawning = false;
    private Sprite currentSyllableSprite;
    private List<GameObject> activeBalloons = new List<GameObject>();
    private Coroutine spawnCoroutine;

    public void StartSpawning(Sprite syllableSprite)
    {
        // Previne múltiplas coroutines
        if (spawning)
        {
            Debug.LogWarning("[BalloonManager] Já está spawnando. Ignorando chamada duplicada.");
            return;
        }

        currentSyllableSprite = syllableSprite;
        spawning = true;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        spawning = false;
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            // Só spawna se não ultrapassar limite
            if (activeBalloons.Count < maxBalloons)
            {
                SpawnOne(currentSyllableSprite);
            }

            yield return new WaitForSeconds(spawnRate);
        }
    }

    void SpawnOne(Sprite syllableSprite)
    {
        if (balloonPrefab == null || spawnPointCenter == null)
        {
            Debug.LogError("[BalloonManager] Prefab ou SpawnPoint não atribuído!");
            return;
        }

        Vector3 spawnPos = spawnPointCenter.position;
        spawnPos.x += Random.Range(-spawnXPadding, spawnXPadding);

        GameObject go = Instantiate(balloonPrefab, spawnPos, Quaternion.identity, transform);
        activeBalloons.Add(go);

        var clickable = go.GetComponent<BalloonClickable>();
        if (clickable != null)
        {
            clickable.SetSyllableSprite(syllableSprite);
            clickable.onFinalPop += () => OnBalloonDestroyed(go);
        }
        else
        {
            Debug.LogError("[BalloonManager] Prefab não tem BalloonClickable!");
        }
    }

    void OnBalloonDestroyed(GameObject go)
    {
        if (activeBalloons.Contains(go))
            activeBalloons.Remove(go);

        onBalloonPopped?.Invoke();
    }

    public void ClearAllBalloons()
    {
        StopSpawning();

        foreach (var b in activeBalloons)
        {
            if (b != null) Destroy(b);
        }
        activeBalloons.Clear();
    }

    private void OnDestroy()
    {
        // Cleanup ao destruir objeto
        StopSpawning();
    }
}