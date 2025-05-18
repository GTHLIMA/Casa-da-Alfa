using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasExploded = false;
    private AudioManager audioManager;

    [SerializeField] private GameObject popupPrefab;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Start()
    {
        if (audioManager != null && CompareTag("Bomb"))
            audioManager.PlaySFX(audioManager.bombFall);

        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (CompareTag("House") && spriteRenderer != null)
        {
            Sprite newSprite = GameManager.Instance.GetCurrentSprite();
            if (newSprite != null) spriteRenderer.sprite = newSprite;
        }
    }

    void Update()
    {
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
                    if (CompareTag("Bomb"))
                    {
                        audioManager.PlaySFX(audioManager.touchImage); 
                        Explode(0);
                    }
                    else if (CompareTag("House"))
                    {
                        GameManager.Instance.ImageTouch();
                        
                        AudioClip spriteAudio = GameManager.Instance.GetCurrentSpriteAudio();
                        if (spriteAudio != null)
                            audioManager.PlaySFX(spriteAudio);

                        Instantiate(popupPrefab, transform.position, Quaternion.identity);
                        Explode(10);
                    }

                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        if (other.CompareTag("Ground"))
        {
            if (CompareTag("Bomb")) audioManager.PlaySFX(audioManager.bombExplosion);

            audioManager.PlaySFX(audioManager.groundFall);
            Explode(0);
        }
    }

    private void Explode(int scoreChange)
    {
        hasExploded = true;

        if (scoreChange != 0)
            GameManager.Instance.AddScore(scoreChange);

        if (spriteRenderer != null && explosionSprite != null)
            spriteRenderer.sprite = explosionSprite;

        if (rb != null)
        {
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
