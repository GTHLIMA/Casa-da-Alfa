using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    [Header("Balloon Settings")]
    public GameObject balloonPrefab;       // Prefab do balão
    public Transform spawnPointCenter;     // Ponto central para spawn (abaixo da tela)
    public float spawnRate = 1.0f;         // Tempo entre spawns
    public float spawnXPadding = 2.0f;     // Largura máxima de variação no eixo X
    public int maxBalloons = 6;            // Limite máximo de balões ativos

    [HideInInspector] public System.Action onBalloonPopped; // Evento que o MainGameManager escuta

    private bool spawning = false;
    private Sprite currentSyllableSprite;  // Sprite da sílaba atual
    private List<GameObject> activeBalloons = new List<GameObject>();

    // Inicia o spawn dos balões
    public void StartSpawning(Sprite syllableSprite)
    {
        currentSyllableSprite = syllableSprite;
        spawning = true;
        StartCoroutine(SpawnRoutine());
    }

    // Para de spawnar
    public void StopSpawning()
    {
        spawning = false;
        StopAllCoroutines();
    }

    // Rotina que instancia balões
    IEnumerator SpawnRoutine()
    {
        while (spawning)
        {
            // Limita quantidade de balões ativos
            if (activeBalloons.Count < maxBalloons)
                SpawnOne(currentSyllableSprite);

            yield return new WaitForSeconds(spawnRate);
        }
    }

    // Spawna um balão na tela
    void SpawnOne(Sprite syllableSprite)
    {
        if (balloonPrefab == null || spawnPointCenter == null) return;

        float randomX = Random.Range(-spawnXPadding, spawnXPadding);
        Vector3 pos = spawnPointCenter.position + new Vector3(randomX, 0f, 0f);

        GameObject go = Instantiate(balloonPrefab, pos, Quaternion.identity, transform);
        activeBalloons.Add(go);

        var clickable = go.GetComponent<BalloonClickable>();
        if (clickable != null)
        {
            clickable.SetSyllableSprite(syllableSprite);
            clickable.onFinalPop += () => OnBalloonDestroyed(go);
        }
    }

    // Quando o balão é estourado
    void OnBalloonDestroyed(GameObject go)
    {
        if (activeBalloons.Contains(go))
            activeBalloons.Remove(go);

        onBalloonPopped?.Invoke(); // avisa o MainGameManager
    }

    // Destroi todos os balões ativos (usado na transição pra fase de fala)
    public void ClearAllBalloons()
    {
        foreach (var b in activeBalloons)
        {
            if (b != null) Destroy(b);
        }
        activeBalloons.Clear();
    }
}
