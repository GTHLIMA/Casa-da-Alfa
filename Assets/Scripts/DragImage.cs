using UnityEngine;

public class DragImage : MonoBehaviour
{
    public float moveSpeed = 50f;

    private bool isDragging;
    private bool hasFallen;
    private bool startedDragging; 
    private Rigidbody2D rb;

    private float minX, maxX;
    private float objectHalfWidth;

    private Vector2 lastMousePosition;
    private Vector2 dragStartPosition;

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
        float distanceToCamera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);

        Vector3 leftBound = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, distanceToCamera));
        Vector3 rightBound = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, distanceToCamera));

        minX = leftBound.x + objectHalfWidth;
        maxX = rightBound.x - objectHalfWidth;
    }

    void OnMouseDown()
    {
        if (hasFallen) return;

        if (audioSource != null && popDownClip != null)
            audioSource.PlayOneShot(popDownClip);

        isDragging = true;
        startedDragging = false;
        dragStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        lastMousePosition = dragStartPosition;
    }

    void OnMouseUp()
    {
        if (hasFallen) return;

        if (startedDragging)
        {
            OnDragRelease();
        }

        isDragging = false;
    }

    void OnDragRelease()
    {
        if (audioSource != null && popUpClip != null)
            audioSource.PlayOneShot(popUpClip);

        hasFallen = true;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.velocity = new Vector2(0f, rb.velocity.y);

        LineToBottom line = GetComponent<LineToBottom>();
        if (line != null)
            line.lineRenderer.enabled = false;
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

            // Só cai se arrastou o suficiente
            if (!startedDragging && Vector2.Distance(currentMousePosition, dragStartPosition) > 0.1f)
            {
                startedDragging = true;
            }

            if (startedDragging)
            {
                float deltaX = currentMousePosition.x - lastMousePosition.x;

                if (Mathf.Abs(deltaX) > 0.01f)
                {
                    float direction = Mathf.Sign(deltaX);
                    rb.velocity = new Vector2(direction * moveSpeed, 0f);
                }
                else
                {
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                }

                lastMousePosition = currentMousePosition;
            }

            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
            transform.position = clampedPosition;
        }
    }
}
