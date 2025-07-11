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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionY;

        objectHalfWidth = GetComponent<SpriteRenderer>().bounds.extents.x;
        float distanceToCamera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);

        Vector3 leftBound = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, distanceToCamera));
        Vector3 rightBound = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, distanceToCamera));

        minX = leftBound.x + objectHalfWidth;
        maxX = rightBound.x - objectHalfWidth;
    }

    void OnMouseDown()
    {
        isDragging = true;
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            rb.constraints = RigidbodyConstraints2D.None;
            isDragging = false;

            // Quando soltar, parar de mover horizontalmente
            rb.velocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 touchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (touchPosition.x < transform.position.x)
            {
                rb.velocity = new Vector2(-moveSpeed, 0f); // Esquerda
            }
            else
            {
                rb.velocity = new Vector2(moveSpeed, 0f);  // Direita
            }
        }
        else
        {
            // Parar de mover caso não esteja arrastando
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        // Impede que ultrapasse os limites da tela
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        transform.position = clampedPosition;
    }
}
