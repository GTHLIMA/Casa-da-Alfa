using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    [Header("Balloon Settings")]
    [Tooltip("Lista de prefabs de balões com cores diferentes (será escolhido aleatoriamente ou por sílaba)")]
    public GameObject[] balloonPrefabs;
    
    [Tooltip("Prefab padrão caso balloonPrefabs esteja vazio")]
    public GameObject defaultBalloonPrefab;
    
    public Transform spawnPointCenter;
    public float spawnRate = 1.0f;
    public float spawnXPadding = 2.0f;
    public int maxBalloons = 6;
    
    [Header("Balloon Color Selection")]
    [Tooltip("Se TRUE, escolhe prefab aleatório. Se FALSE, usa o prefab específico da sílaba")]
    public bool randomizeBalloonColor = false;

    [HideInInspector] public System.Action onBalloonPopped;
    [HideInInspector] public System.Action<Vector2> onBalloonPoppedWithPosition;

    private bool spawning = false;
    private SyllableDado currentSyllableData;
    private List<GameObject> activeBalloons = new List<GameObject>();
    private Coroutine spawnCoroutine;

    public void StartSpawning(SyllableDado syllableData)
    {
        if (spawning)
        {
            Debug.LogWarning("[BalloonManager] Já está spawnando. Ignorando chamada duplicada.");
            return;
        }

        currentSyllableData = syllableData;
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
            if (activeBalloons.Count < maxBalloons)
            {
                SpawnOne(currentSyllableData);
            }

            yield return new WaitForSeconds(spawnRate);
        }
    }

    void SpawnOne(SyllableDado syllableData)
    {
        if (spawnPointCenter == null)
        {
            Debug.LogError("[BalloonManager] SpawnPoint não atribuído!");
            return;
        }

        // 🎨 ESCOLHE QUAL PREFAB DE BALÃO USAR
        GameObject prefabToUse = GetBalloonPrefab(syllableData);
        
        if (prefabToUse == null)
        {
            Debug.LogError("[BalloonManager] Nenhum prefab de balão disponível!");
            return;
        }

        Vector3 spawnPos = spawnPointCenter.position;
        spawnPos.x += Random.Range(-spawnXPadding, spawnXPadding);

        GameObject go = Instantiate(prefabToUse, spawnPos, Quaternion.identity, transform);
        activeBalloons.Add(go);

        var clickable = go.GetComponent<BalloonClickable>();
        if (clickable != null)
        {
            clickable.SetSyllableData(syllableData);
            clickable.onFinalPop += () => OnBalloonDestroyed(go);
            clickable.onBalloonPoppedWithPosition += (position) => OnBalloonPoppedWithPosition(position);
        }
        else
        {
            Debug.LogError("[BalloonManager] Prefab não tem BalloonClickable!");
        }
    }

    // 🎨 MÉTODO QUE ESCOLHE O PREFAB DO BALÃO
    GameObject GetBalloonPrefab(SyllableDado syllableData)
    {
        // Se não há lista de prefabs, usa o padrão
        if (balloonPrefabs == null || balloonPrefabs.Length == 0)
        {
            return defaultBalloonPrefab;
        }

        // Modo ALEATÓRIO: escolhe qualquer prefab
        if (randomizeBalloonColor)
        {
            int randomIndex = Random.Range(0, balloonPrefabs.Length);
            return balloonPrefabs[randomIndex];
        }

        // Modo ESPECÍFICO: usa o índice da sílaba para escolher o prefab
        var mm = MainGameManager.Instance;
        if (mm != null)
        {
            int syllableIndex = mm.currentSyllableIndex;
            int prefabIndex = syllableIndex % balloonPrefabs.Length; // Cicla entre os prefabs
            return balloonPrefabs[prefabIndex];
        }

        // Fallback: primeiro da lista
        return balloonPrefabs[0];
    }

    void OnBalloonDestroyed(GameObject go)
    {
        if (activeBalloons.Contains(go))
            activeBalloons.Remove(go);
    }

    void OnBalloonPoppedWithPosition(Vector2 position)
    {
        onBalloonPoppedWithPosition?.Invoke(position);
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
        StopSpawning();
    }
}