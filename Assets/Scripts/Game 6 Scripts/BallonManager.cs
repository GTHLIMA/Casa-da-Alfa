using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    public GameObject balloonPrefab; // prefab should contain BalloonClickable component
    public float spawnRate = 1.0f;
    public float spawnXPadding = 1.0f;
    public Transform spawnPointCenter;
    public bool spawning = false;
    public event Action onBalloonPopped; // fired by BalloonClickable when final pop happens

    private float maxVisibleX;
    private List<GameObject> activeBalloons = new List<GameObject>();

    private Coroutine spawnCoroutine;

    private void Awake()
    {
        maxVisibleX = Camera.main.orthographicSize * Camera.main.aspect;
    }

    public void StartSpawning(Sprite syllableSprite)
    {
        if (spawning) return;
        spawning = true;
        spawnCoroutine = StartCoroutine(SpawnLoop(syllableSprite));
    }

    public void StopSpawning()
    {
        spawning = false;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
    }

    IEnumerator SpawnLoop(Sprite syllableSprite)
    {
        yield return new WaitForSeconds(0.2f);
        while (spawning)
        {
            SpawnOne(syllableSprite);
            yield return new WaitForSeconds(spawnRate);
        }
    }

    void SpawnOne(Sprite syllableSprite)
    {
        if (balloonPrefab == null) return;
        Vector3 pos = spawnPointCenter.position;
        pos.x = UnityEngine.Random.Range(-maxVisibleX + spawnXPadding, maxVisibleX - spawnXPadding);
        pos.y = spawnPointCenter.position.y;
        GameObject go = Instantiate(balloonPrefab, pos, Quaternion.identity, transform);
        var clickable = go.GetComponent<BalloonClickable>();
        if (clickable != null) clickable.SetSyllableSprite(syllableSprite);
        clickable.onFinalPop += () => onBalloonPopped?.Invoke();
        activeBalloons.Add(go);
    }

    public void ClearAllBalloons()
    {
        foreach (var b in activeBalloons) if (b != null) Destroy(b);
        activeBalloons.Clear();
    }
}
