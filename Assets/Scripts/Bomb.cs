using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasExploded = false;

    void Start()
    {
         // Get the SpriteRenderer and Rigidbody2D components
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (hasExploded) return;

         // Only start if thouched the screen
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D hit = Physics2D.OverlapPoint(touchPosition);

                if (hit != null && hit.transform == transform)
                {
                    int scoreChange = 0;

                    if (CompareTag("House"))
                        scoreChange = 10;
                    else if (CompareTag("Bomb"))
                        scoreChange = -5;
                    Explode(scoreChange);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Ground"))
        {
            Explode(0);
        }
    }

    private void Explode(int scoreChange)
    {
        hasExploded = true;

        if (scoreChange != 0)
            GameManager.Instance.AddScore(scoreChange);

         // Change the sprite to the explosion sprite
        if (spriteRenderer != null && explosionSprite != null)
            spriteRenderer.sprite = explosionSprite;

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
