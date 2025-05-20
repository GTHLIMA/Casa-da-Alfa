using UnityEngine;

public class FallingItem : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private AudioClip[] audios;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private AudioManager audioManager;
    private bool hasHitGround = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    public void Initialize(int index)
    {
        if (sprites.Length == 0 || index >= sprites.Length) return;

        spriteRenderer.sprite = sprites[index];

        if (index < audios.Length && audios[index] != null && audioManager != null)
            audioManager.PlaySFX(audios[index]);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitGround) return;

        if (other.CompareTag("Ground"))
        {
            hasHitGround = true;
            Destroy(gameObject, 0.3f); // Ou use uma animação de impacto antes
        }
    }
}
