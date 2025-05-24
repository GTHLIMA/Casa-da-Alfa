using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private Sprite explosionSprite;
    [SerializeField] private float explosionDelay = 0.3f;
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private GameObject handAnimationPrefab; // NOVO: Referência para o Prefab da Mão

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasExploded = false;
    private AudioManager audioManager;

    private float destructTime = 1f;
    private float timer;

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

        timer = destructTime;
    }

    void Update()
    {
        if (hasExploded) return;

        // --- VERIFICA TOQUE ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleInteraction(touch.position); // MODIFICADO: Chama uma função para lidar com o toque/clique
            }
        }
        // --- VERIFICA CLIQUE (PARA TESTE) ---
        if (Input.GetMouseButtonDown(0))
        {
             HandleInteraction(Input.mousePosition); // MODIFICADO: Chama a mesma função
        }
        // --- FIM VERIFICAÇÕES ---


        // MODIFICADO: A lógica de destruição por tempo foi removida do Update
        // Se precisar dela, talvez precise repensar onde ela se encaixa com a nova lógica.
        /*
        if (CompareTag("House") && hasExploded) // Checagem se deve destruir por tempo
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }
        */
    }

    // NOVO: Função centralizada para lidar com o toque ou clique
    void HandleInteraction(Vector3 screenPosition)
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        if (hit != null && hit.transform == transform && !hasExploded) // Garante que não foi explodido ainda
        {
            if (CompareTag("Bomb"))
            {
                audioManager.PlaySFX(audioManager.touchImage);
                GameManager.Instance.BombTouch();
                Explode(0, false); // MODIFICADO: Passa 'false' para indicar que não é 'House'
            }
            else if (CompareTag("House"))
            {
                audioManager.PlaySFX(audioManager.touchImage);
                GameManager.Instance.ImageTouch();
                Instantiate(popupPrefab, transform.position, Quaternion.identity);

                // NOVO: Instancia a animação da mão na posição do item
                if (handAnimationPrefab != null)
                {
                    Instantiate(handAnimationPrefab, transform.position, Quaternion.identity);
                }

                Explode(10, true); // MODIFICADO: Passa 'true' para indicar que é 'House'
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
            Explode(0, false); // MODIFICADO: Passa 'false'
        }
    }

    // MODIFICADO: Adicionado 'bool isHouse' para saber como explodir/sumir
    private void Explode(int scoreChange, bool isHouse)
    {
        hasExploded = true;

        if (scoreChange != 0)
            GameManager.Instance.AddScore(scoreChange);

        if (rb != null)
        {
            rb.velocity = Vector2.zero; // MODIFICADO: Zera a velocidade
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }

        // MODIFICADO: Lógica de sprite/visibilidade
        if (spriteRenderer != null)
        {
            if (isHouse)
            {
                // Se for 'House', apenas o torna invisível imediatamente.
                // A animação da mão aparecerá no lugar.
                spriteRenderer.enabled = false;
            }
            else if (explosionSprite != null)
            {
                // Se for 'Bomb', mostra a explosão.
                spriteRenderer.sprite = explosionSprite;
            }
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Se for 'House', ele já ficou invisível, então o delay
        // é apenas para limpar o GameObject da cena.
        // Se for 'Bomb', é o tempo que a explosão fica na tela.
        yield return new WaitForSeconds(explosionDelay);
        Destroy(gameObject);
    }
}