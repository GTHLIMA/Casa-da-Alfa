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
        if (!GameManager.GameStarted && (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0)))
        {
            GameManager.GameStarted = true;

            // Melhorar depois!!!!!!!!!!!!!!!!!!!
            // // Toca a música de fundo
            // if (audioManager != null) 
            //     audioManager.PlayAudio(audioManager.background);

            // // Destroi o objeto com tag "teste"
            // GameObject handDrag = GameObject.FindGameObjectWithTag("teste");
            // if (handDrag != null) 
            //     Destroy(handDrag);
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

            if (floatingPoints != null)
            {
                GameObject points = Instantiate(floatingPoints, transform.position, Quaternion.identity);
                points.transform.GetChild(0).GetComponent<TextMesh>().text = "+10";
            }
            else
            {

                Debug.LogError("ERRO: A variável 'floatingPoints' não está atribuída no Inspector do Balão: " + gameObject.name);
            }


            GameManager.Instance.AddScore(10);
            audioManager.PlaySFX(audioManager.ballonPop);
            Destroy(gameObject);

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
} 