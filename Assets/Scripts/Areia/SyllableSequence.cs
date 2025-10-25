using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class SyllableSequence : MonoBehaviour
{
    [Header("Sprites na ordem")]
    public List<Sprite> syllableSprites;

    SpriteRenderer sr;
    PolygonCollider2D poly;
    LetterTracer tracer;
    int currentIndex = 0;

void Awake()
{
    // Cria manager se não existir
    if (ProgressManager.Instance == null)
    {
        GameObject go = new GameObject("Managers");
        go.AddComponent<ProgressManager>();
        go.AddComponent<TouchManager>();
    }
}

    void Start()
    {
        StartCoroutine(WaitAndInit());
    }

    IEnumerator WaitAndInit()
{
    yield return null; // 1 frame

    sr   = GetComponent<SpriteRenderer>();
    poly = GetComponent<PolygonCollider2D>();
    tracer = GetComponent<LetterTracer>();
    ProgressManager.Instance.OnLetterCompleted.AddListener(OnSyllableCompleted);

    if (syllableSprites == null || syllableSprites.Count == 0 || syllableSprites[0] == null)
    {
        Debug.LogWarning("Nenhuma sílaba atribuída – adicione sprites na lista!");
        yield break;
    }
    LoadSyllable(0);
}

    void LoadSyllable(int idx)
    {
        if (idx >= syllableSprites.Count) { EndSequence(); return; }

        tracer.ClearAllStrokes();
        tracer.ResetTrace();

        currentIndex = idx;
        Sprite spr = syllableSprites[idx];
        sr.sprite = spr;

        int pathCount = spr.GetPhysicsShapeCount();
        poly.pathCount = pathCount;
        for (int i = 0; i < pathCount; i++)
        {
            List<Vector2> path = new List<Vector2>();
            spr.GetPhysicsShape(i, path);
            poly.SetPath(i, path);
        }
        tracer.ReloadPaths();
    }

    void OnSyllableCompleted(char _) => Invoke(nameof(Next), 0.5f);
    void Next() => LoadSyllable(currentIndex + 1);

    void EndSequence()
    {
        Panels.Instance.ShowEndPhasePanel();
        Debug.Log("Todas as sílabas concluídas!");

    }
}