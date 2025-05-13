using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slingshot : MonoBehaviour
{
    public Transform projectile;
    public Rigidbody2D rb;
    public LineRenderer lineRenderer;

    private Vector3 startPosition;
    private bool isDragging = false;
    public bool isReadyToShoot = true; // <--variável de controle
    public float forceMultiplier = 10f;

    [Header("Drag Limit")]
    public float maxDragDistance = 1;

    private void Start()
    {
        startPosition = transform.position + new Vector3(0, 0.2f, 0);
        rb = projectile.GetComponent<Rigidbody2D>();
        ResetProjectile(); // já começa preparado
    }

    private void Update()
    {
        if (!isReadyToShoot) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Vector2.Distance(mouseWorldPos, projectile.position) < 1f)
                isDragging = true;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // will limit how far you can drag the stone
            Vector3 toMouse = mousePos - startPosition;
            if (toMouse.magnitude > maxDragDistance)
            {
                toMouse = toMouse.normalized * maxDragDistance;
            }

            projectile.position = startPosition + toMouse;

            DrawLine(projectile.position);
        }


        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            Vector2 force = (startPosition - projectile.position) * forceMultiplier;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = force;
            isReadyToShoot = false;
            lineRenderer.enabled = false;

            Invoke(nameof(ResetProjectile), 1f); // Reset the projectile after 1 second
        }
    }

    void DrawLine(Vector3 dragPosition)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, dragPosition);
    }

    // sera chamado pelo outro script (Projectile.cs)
    public void ResetProjectile()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        projectile.position = startPosition;
        isReadyToShoot = true;
        lineRenderer.enabled = false;
    }
}
