using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class LetterTracer : MonoBehaviour
{
    [Header("Referencias")]
    public LineRenderer traceLine;
    public ParticleSystem fx;
    public Gradient paintGradient;
    public AudioSource scribbleAudio;
    public AudioSource successAudio;

    [Header("Stroke prefab")]
    public GameObject strokePrefab;
    public Transform strokeRoot;
    public bool hideOnPause = true; 
    public bool hideOnEndPhase = true;

    [Header("Config")]
    public float completionThreshold = 1f;
    public float pointSpacing = 0f;
    public float drawingZOffset = -1f; 

    [Header("Volume")]
    public float scribbleVolume = 0.3f;   
    
    PolygonCollider2D poly;
    public List<Vector2> originalPoints;
    public List<bool> hitFlags;
    public int hitsNeeded;
    public int hitsSoFar;

    List<LineRenderer> allStrokes = new List<LineRenderer>();
    public LineRenderer currentStrokeLR;
    bool isDrawing;
    public int passCount = 0;
    public bool clearPending = false;

    void Update()
    {
        if (hideOnPause)
        {
            bool show = Time.timeScale > 0f;
            foreach (var lr in allStrokes)
                if (lr != null) lr.gameObject.SetActive(show);
        }
    }

    void Awake()
    {
        poly = GetComponent<PolygonCollider2D>();
        originalPoints = new List<Vector2>();
        hitFlags = new List<bool>();
        hitsNeeded = 0;
        hitsSoFar = 0;
        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void ReloadPaths()
    {
        originalPoints.Clear();
        hitFlags.Clear();
        for (int i = 0; i < poly.pathCount; i++)
            originalPoints.AddRange(poly.GetPath(i));

        hitsNeeded = originalPoints.Count;
        hitFlags = new List<bool>(hitsNeeded);
        for (int j = 0; j < hitsNeeded; ++j) hitFlags.Add(false);
        hitsSoFar = 0;
    }

    void OnEnable()
    {
        TouchManager.OnFingerDown += StartDraw;
        TouchManager.OnFingerMove += MoveDraw;
        TouchManager.OnFingerUp += EndDraw;
    }
    
    void OnDisable()
    {
        TouchManager.OnFingerDown -= StartDraw;
        TouchManager.OnFingerMove -= MoveDraw;
        TouchManager.OnFingerUp -= EndDraw;
    }

    void StartDraw(Vector2 world)
    {
        if (passCount == 2)               
        {
            ClearAllStrokes();
            ResetTrace();
            passCount = 0;                  
            return;                         
        }

        isDrawing = true;
        passCount++;

        GameObject go = Instantiate(strokePrefab, strokeRoot);
        currentStrokeLR = go.GetComponent<LineRenderer>();
        
        Vector3 worldPos = new Vector3(world.x, world.y, drawingZOffset);
        
        currentStrokeLR.positionCount = 1;
        currentStrokeLR.SetPosition(0, worldPos);
        allStrokes.Add(currentStrokeLR);

        if (passCount == 1)
            currentStrokeLR.startColor = currentStrokeLR.endColor = Color.white;
        else if (passCount == 2)
            currentStrokeLR.startColor = currentStrokeLR.endColor = paintGradient.Evaluate(0);

        fx.transform.position = worldPos;
        fx.Play();

        if (scribbleAudio != null)
        {
            scribbleAudio.Stop();
            scribbleAudio.volume = scribbleVolume;
            scribbleAudio.Play();
        }
    }

    void MoveDraw(Vector2 world)
    {
        if (!isDrawing) return;

        Vector3 worldPos = new Vector3(world.x, world.y, drawingZOffset);
        
        int count = currentStrokeLR.positionCount;
        currentStrokeLR.positionCount = count + 1;
        currentStrokeLR.SetPosition(count, worldPos);

        fx.transform.position = worldPos;
        CheckCoverage(world);
    }

    void EndDraw(Vector2 world)
    {
        isDrawing = false;
        fx.Stop();
        if (scribbleAudio) scribbleAudio.Pause();

        if (passCount == 2)             
        {
            float pct = (float)hitsSoFar / hitsNeeded;
            if (pct >= completionThreshold)
            {
                FinishLetter();
                passCount = 2;             
            }
        }
    }

    void CheckCoverage(Vector2 p)
    {
        if (!poly.OverlapPoint(p)) return;

        float checkRadius = 0.08f;
        for (int i = 0; i < originalPoints.Count; i++)
        {
            if (hitFlags[i]) continue;
            if (Vector2.Distance(p, originalPoints[i]) < checkRadius)
            {
                hitFlags[i] = true;
                hitsSoFar++;
                UpdateVisualProgress();
            }
        }
    }

    void UpdateVisualProgress()
    {
        float t = (float)hitsSoFar / hitsNeeded;
        Color c = paintGradient.Evaluate(t);
        foreach (var lr in allStrokes) lr.startColor = lr.endColor = c;
    }

    void FinishLetter()
    {
        Color c = paintGradient.Evaluate(1);
        foreach (var lr in allStrokes) lr.startColor = lr.endColor = c;
        if (successAudio) successAudio.Play();
        fx.Emit(30);
        ProgressManager.Instance.MarkCompleted(gameObject.name[0]);
    }

    public void ClearAllStrokes()
    {
        foreach (var lr in allStrokes) Destroy(lr.gameObject);
        allStrokes.Clear();
    }

    public void ResetTrace()
    {
        hitsSoFar = 0;
        for (int i = 0; i < hitFlags.Count; ++i) hitFlags[i] = false;
    }
}