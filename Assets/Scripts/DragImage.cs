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
        if (audioSource != null && popDownClip != null)
        {
            audioSource.PlayOneShot(popDownClip);
        }

        isDragging = true;
        lastMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Congela Y e rotação para evitar queda enquanto arrasta
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    }

    void OnMouseUp()
    {
        OnDragRelease();
    }

    void OnDragRelease()
    {
        if (audioSource != null && popUpClip != null)
        {
            audioSource.PlayOneShot(popUpClip);
        }

        if (isDragging)
        {
            isDragging = false;

            // Remove restrição de Y para permitir queda natural
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Para o movimento horizontal ao soltar, mantém velocidade Y
            rb.velocity = new Vector2(0f, rb.velocity.y);

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
        // Detecta fim do toque/drag em dispositivos touch e mouse
        if (isDragging)
        {
            bool released = false;

            // Mouse
            if (Input.GetMouseButtonUp(0))
                released = true;

            // Touch
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    released = true;
            }

            if (released)
            {
                OnDragRelease();
            }
        }

        // Movimentação enquanto arrasta
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
        else if (!isDragging)
        {
            // Permite queda natural, sem restrição em Y
            // Aqui não precisa setar nada porque Rigidbody já está com constraints só na rotação
        }

        // Limita posição X para ficar dentro da tela
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        transform.position = clampedPosition;
    }
}
