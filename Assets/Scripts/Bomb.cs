using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool hasExploded = false;
    private AudioManager audioManager;

    public GameObject floatingPoints;

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
        boxCollider = GetComponent<BoxCollider2D>();

        if (CompareTag("House") && spriteRenderer != null)
        {
            Sprite newSprite = GameManager.Instance.GetCurrentSprite();
            if (newSprite != null) spriteRenderer.sprite = newSprite;
        }
    }

    void Update()
    {
        if (hasExploded) return;

        AdjustColliderSize();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                RaycastHit2D hit = Physics2D.Raycast(touchPosition, Vector2.zero);


                if (hit.collider != null && hit.transform == transform)
                {
                    if (CompareTag("Bomb"))
                    {
                        GameManager.Instance.bombTouchCount++;

                        if (GameManager.Instance.bombTouchCount >= 3)
                        {
                            audioManager.PlaySFX(audioManager.warning);
                            GameManager.Instance.bombTouchCount = 0;
                            Explode(0);
                        }
                        else
                        {
                            audioManager.PlaySFX(audioManager.bombExplosion);
                            Explode(0);
                        }
                    }
                    else if (CompareTag("House"))
                    {
                        AudioClip spriteAudio = GameManager.Instance.GetCurrentSpriteAudio();
                        if (spriteAudio != null) audioManager.PlaySFX(spriteAudio);

                        GameManager.Instance.ImageTouch();

                        GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
                        points.transform.GetChild(0).GetComponent<TextMesh>().text = "+10";
                        Explode(10);
                    }

                }
            }
        }
    }

    private void AdjustColliderSize()
    {
        if (rb != null || boxCollider == null) return;

        float fallSpeed = rb.velocity.y;

        if (fallSpeed < -2f)
        {
            boxCollider.size = new Vector2(1f, 2f);
            boxCollider.offset = new Vector2(0f, 0.5f);
        }
        else
        {
            boxCollider.size = new Vector2(1f, 1f);
            boxCollider.offset = new Vector2(0f, 0.16f);
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
