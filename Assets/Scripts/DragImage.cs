using UnityEngine;

public class DragImage : MonoBehaviour
{
    public float moveSpeed = 50f;
    public float dragThreshold = 0.1f;

    private bool isDragging;
    private bool hasFallen;
    private Rigidbody2D rb;

    private float minX, maxX;
    private float objectHalfWidth;
    private float initialYPosition;

    private Vector2 dragStartPosition;
    private Vector2 previousMousePosition;
    private float dragStartTime;
    private float totalDragTime;
    private bool isCurrentlyDragging;

    private AudioSource audioSource;

    [Header("Sons")]
    public AudioClip popDownClip;
    public AudioClip popUpClip;
    public AudioClip popOtherClip;

    [Header("AudioSource Extra para colisão")]
    public AudioSource otherAudioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        audioSource = GetComponent<AudioSource>();

        objectHalfWidth = GetComponent<SpriteRenderer>().bounds.extents.x;
        CalculateScreenBounds();
        
        initialYPosition = transform.position.y;
    }

    void CalculateScreenBounds()
    {
        float distanceToCamera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        Vector3 leftBound = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, distanceToCamera));
        Vector3 rightBound = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, distanceToCamera));

        minX = leftBound.x + objectHalfWidth;
        maxX = rightBound.x - objectHalfWidth;
    }

    void OnMouseDown()
    {
        if (hasFallen) return;

        var logger = FindObjectOfType<DragGameLogger>();
        logger?.LogDragStart(GetComponent<SpriteRenderer>().sprite.name, transform.position);

        if (audioSource != null && popDownClip != null)
            audioSource.PlayOneShot(popDownClip);

        isDragging = true;
        isCurrentlyDragging = true;
        dragStartTime = Time.time;
        totalDragTime = 0f;    
        dragStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        previousMousePosition = dragStartPosition;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.velocity = Vector2.zero;
    }

    void OnMouseUp()
    {
        if (!isDragging || hasFallen) return;

        var logger = FindObjectOfType<DragGameLogger>();
        Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float dragDistance = Vector2.Distance(currentMousePosition, dragStartPosition);
        bool wasDropped = dragDistance > dragThreshold;

        // Calcula o tempo total de arrasto
        if (isCurrentlyDragging)
        {
            totalDragTime = Time.time - dragStartTime;
            isCurrentlyDragging = false;
        }

        // Log com informações detalhadas do arrasto
        logger?.LogDragEnd(
            GetComponent<SpriteRenderer>().sprite.name, 
            dragStartPosition, 
            currentMousePosition, 
            wasDropped,
            totalDragTime,
            dragDistance
        );

        if (dragDistance > dragThreshold)
        {
            ReleaseObject();
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            isDragging = false;
        }
    }

    void OnMouseDrag()
    {
        if (isCurrentlyDragging)
        {
            totalDragTime = Time.time - dragStartTime;
        }
    }

    void ReleaseObject()
    {
        if (audioSource != null && popUpClip != null)
            audioSource.PlayOneShot(popUpClip);

        isDragging = false;
        isCurrentlyDragging = false;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.zero;
        rb.velocity = new Vector2(0f, -2f);

        DisableLineRenderer();
    }

    void CheckForFall()
    {
        if (!hasFallen && rb != null)
        {
            if (rb.velocity.y < -0.1f)
            {
                hasFallen = true;
                Debug.Log("Objeto caiu de verdade! Velocidade Y: " + rb.velocity.y);

                DisableLineRenderer();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }

    void DisableLineRenderer()
    {
        var lineToBottom = GetComponent<LineToBottom>();
        if (lineToBottom != null && lineToBottom.lineRenderer != null)
        {
            lineToBottom.lineRenderer.enabled = false;
            return;
        }

        var lr = GetComponent<LineRenderer>() ?? GetComponentInChildren<LineRenderer>();
        if (lr != null)
        {
            lr.enabled = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (otherAudioSource != null && popOtherClip != null)
            otherAudioSource.PlayOneShot(popOtherClip);
    }

    void Update()
    {
        if (isDragging && !hasFallen)
        {
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            Vector2 positionDelta = currentMousePosition - previousMousePosition;
            
            Vector3 newPosition = transform.position + new Vector3(positionDelta.x, 0f, 0f);
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            transform.position = newPosition;
            
            previousMousePosition = currentMousePosition;
        }

        CheckForFall();
    }

    void FixedUpdate()
    {
        CheckForFall();
    }

    public void ResetObject()
    {
        hasFallen = false;
        isDragging = false;
        isCurrentlyDragging = false;

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = Vector2.zero;
        initialYPosition = transform.position.y;
        
        var lineToBottom = GetComponent<LineToBottom>();
        if (lineToBottom != null && lineToBottom.lineRenderer != null)
            lineToBottom.lineRenderer.enabled = true;
    }
}