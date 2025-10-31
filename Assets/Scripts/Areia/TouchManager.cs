using UnityEngine;

public class TouchManager : MonoBehaviour
{
    public static event System.Action<Vector2> OnFingerDown;
    public static event System.Action<Vector2> OnFingerMove;
    public static event System.Action<Vector2> OnFingerUp;

    public GameObject PauseMenu;
    public GameObject endPhasePanel;
    public ParticleSystem confettiEffect;

    private Camera _cam;                
    private Camera cam                         
    {
        get
        {
            if (_cam == null) _cam = Camera.main;
            return _cam;
        }
    }

    void Awake()
    {
        _cam = Camera.main;                  
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector2 w = cam.ScreenToWorldPoint(t.position);
            switch (t.phase)
            {
                case TouchPhase.Began: OnFingerDown?.Invoke(w); break;
                case TouchPhase.Moved: OnFingerMove?.Invoke(w); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: OnFingerUp?.Invoke(w); break;
            }
            return;
        }

        Vector2 m = cam.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0)) OnFingerDown?.Invoke(m);
        else if (Input.GetMouseButton(0)) OnFingerMove?.Invoke(m);
        else if (Input.GetMouseButtonUp(0)) OnFingerUp?.Invoke(m);
    }
}