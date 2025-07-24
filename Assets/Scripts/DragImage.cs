using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragImage : MonoBehaviour
{
    public float moveSpeed = 5f;
    private bool isDragging;
    private Rigidbody2D rb;

    private float minX, maxX;
    private float objectHalfWidth;

    private Vector2 lastMousePosition;

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
        rb.constraints = RigidbodyConstraints2D.FreezePositionY;

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
        if (audioSource != null && popDownClip != null)
        {
            audioSource.PlayOneShot(popDownClip);
        }

        isDragging = true;
        lastMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    void OnMouseUp()
    {
        if (audioSource != null && popUpClip != null)
        {
            audioSource.PlayOneShot(popUpClip);
        }

        if (isDragging)
        {
            isDragging = false;
            rb.constraints = RigidbodyConstraints2D.None;
            rb.velocity = Vector2.zero;

            LineToBottom line = GetComponent<LineToBottom>();
            if (line != null)
            {
                line.lineRenderer.enabled = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("Colidiu com: " + other.name);

        if (otherAudioSource != null && popOtherClip != null)
        {
            otherAudioSource.PlayOneShot(popOtherClip);
        }
    }


    void Update()
    {
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
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
        else
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        transform.position = clampedPosition;
    }
}
