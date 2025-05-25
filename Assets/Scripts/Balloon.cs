using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed;
    private AudioManager audioManager;
    private GameManager gameManager;

    [SerializeField] private GameObject spriteDropPrefab;
    [SerializeField] private Sprite[] dropSprites; 
    [SerializeField] private AudioClip[] dropAudioClips; 
    public GameObject floatingPoints;

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Update()
    {
        // Controle por toque (para mobile)
        if (!GameManager.GameStarted && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                GameManager.GameStarted = true; 
            }
        }


        if (!GameManager.GameStarted && Input.GetMouseButtonDown(0))
        {
            GameManager.GameStarted = true;
        }

        if (transform.position.y > 7f)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
       if (GameManager.GameStarted) transform.Translate(0, upSpeed, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Slingshot"))
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb != null && rb.velocity.magnitude > 1f)
        {
            // --- MODIFICADO: Adiciona checagem ---
            if (floatingPoints != null)
            {
                GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
                points.transform.GetChild(0).GetComponent<TextMesh>().text = "+10";
            }
            else
            {
                // Mostra um erro claro no console se não estiver atribuído
                Debug.LogError("ERRO: A variável 'floatingPoints' não está atribuída no Inspector do Balão: " + gameObject.name);
            }
            // --- FIM DA MODIFICAÇÃO ---

            GameManager.Instance.AddScore(10);
            audioManager.PlaySFX(audioManager.ballonPop);

            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.CallResetFromSlingshot();
            }

            DropNextSprite();
        }
    }
}

    private void DropNextSprite()
    {
        if (dropSprites.Length == 0 || spriteDropPrefab == null) return;

        if (GameManager.CurrentDropIndex >= dropSprites.Length)
        {
            GameManager.CurrentDropIndex = 0;
        }

        GameObject dropInstance = Instantiate(spriteDropPrefab, transform.position, Quaternion.identity);

        SpriteRenderer sr = dropInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = dropSprites[GameManager.CurrentDropIndex];
        }

        if (GameManager.CurrentDropIndex < dropAudioClips.Length && dropAudioClips[GameManager.CurrentDropIndex] != null)
        {
            audioManager.PlaySFX(dropAudioClips[GameManager.CurrentDropIndex]);
        }
        Destroy(dropInstance, 4f);

        GameManager.CurrentDropIndex++; 
        GameManager.Instance.CheckEndPhase(GameManager.CurrentDropIndex, dropSprites.Length);
    }




    // private void ResetPosition()
    // {
    //     float safeMargin = 0.5f; 
    //     float screenHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
    //     float randomX = Random.Range(-screenHalfWidth + safeMargin, screenHalfWidth - safeMargin);
    //     float posY = -7f; 

    //     if (gameObject.name.Contains("RedBallon")) posY = -7f;
        
    //     else if (gameObject.name.Contains("YellowBallon")) posY = -12f;
        
    //     else if (gameObject.name.Contains("PinkBallon")) posY = -17f;
        

    //     transform.position = new Vector2(randomX, posY);
    // }
} 