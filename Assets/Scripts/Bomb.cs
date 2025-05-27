using System.Collections;
using UnityEngine;
using TMPro; 

public class Bomb : MonoBehaviour
{
    [Header("Configurações Base")]
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private GameObject handAnimationPrefab;


    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasExploded = false;
    private AudioManager audioManager;
    private int pointsToAward = 10; 
    public bool isRareItem = false; 


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

        // Se for "House", define o sprite vindo do GameManager
        // O material dourado será aplicado pelo GameManager se for raro.
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
                HandleInteraction(touch.position);
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
             HandleInteraction(Input.mousePosition);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded || !other.CompareTag("Ground")) return;

        hasExploded = true;

        if (CompareTag("Bomb"))
        {
            Debug.Log("Bomba atingiu o chão. Explodindo sem pontos.");
            audioManager.PlaySFX(audioManager.bombExplosion);
            Explode(0, false);
        }
        else if (CompareTag("House"))
        {
            Debug.Log("Casa atingiu o chão. Destruindo sem pontos.");
            audioManager.PlaySFX(audioManager.groundFall);
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            Destroy(gameObject, 0.2f);
        }
    }


    public void SetAsRare(int rareScoreValue)
    {
        pointsToAward = rareScoreValue;
        isRareItem = true;
        Debug.Log("Item " + gameObject.name + " definido como Raro. Pontos: " + pointsToAward);
    }

    void HandleInteraction(Vector3 screenPosition)
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        if (hit != null && hit.transform == transform && !hasExploded)
        {
            if (CompareTag("Bomb"))
            {
                audioManager.PlaySFX(audioManager.touchImage);
                GameManager.Instance.BombTouch();
                Explode(0, false);
            }
            else if (CompareTag("House"))
            {
                audioManager.PlaySFX(audioManager.touchImage);
                GameManager.Instance.ImageTouch();

                // --- MODIFICAÇÃO PARA O TEXTO DO POPUP ---
                // 1. Instancia o popup E guarda a referência
                GameObject popupInstance = Instantiate(popupPrefab, transform.position, Quaternion.identity);

                // 2. Tenta encontrar e configurar o texto (TMP ou TextMesh)
                TextMeshProUGUI tmpText = popupInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = "+" + pointsToAward.ToString(); // Usa pointsToAward (10 ou 50)
                }
                else
                {
                    TextMesh tmText = popupInstance.GetComponentInChildren<TextMesh>();
                    if (tmText != null)
                    {
                        tmText.text = "+" + pointsToAward.ToString(); // Usa pointsToAward (10 ou 50)
                    }
                    else
                    {
                        Debug.LogWarning("Não foi possível encontrar TextMeshProUGUI ou TextMesh no popupPrefab!");
                    }
                }

                // Instancia a animação da mão
                if (handAnimationPrefab != null)
                {
                    Instantiate(handAnimationPrefab, transform.position, Quaternion.identity);
                }

                // Explode, passando os pontos corretos
                Explode(pointsToAward, true);
            }
        }
    }

    private void Explode(int scoreChange, bool isHouse)
    {
        hasExploded = true;

        if (scoreChange != 0)
            GameManager.Instance.AddScore(scoreChange);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (spriteRenderer != null)
        {
            if (isHouse)
            {
                spriteRenderer.enabled = false;
            }
            else if (explosionSprite != null)
            {
                spriteRenderer.sprite = explosionSprite;
            }
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Destroy(gameObject);
    }
}