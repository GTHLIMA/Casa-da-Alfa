using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasScored = false;
    private bool hasExploded = false;

    void Start()
    {
        // Get the SpriteRenderer and Rigidbody2D components
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Only start if thouched the screen
        if (hasExploded) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D hit = Physics2D.OverlapPoint(touchPosition);
                if (hit != null && hit.transform == transform)
                {
                    Explode(false);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Ground") && !hasScored)
        {
            hasScored = true;
            Explode(true);
        }
    }

    private void Explode(bool addScore)
    {
        hasExploded = true;

        if (addScore)
            GameManager.Instance.AddScore(1);

        // Change the sprite to the explosion sprite
        if (spriteRenderer != null && explosionSprite != null)
            spriteRenderer.sprite = explosionSprite;

        // 
        if (rb != null)
        {
            // Stop the Rigidbody2D from moving 
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Destroy(gameObject);
    }
}
