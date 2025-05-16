using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed;
    private bool gameStarted = false;
    private AudioManager audioManager;

    [SerializeField] private GameObject[] balloonDropPrefabs; 

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Update()
    {

        if (!gameStarted && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                gameStarted = true; 
            }
        }

        if (transform.position.y > 7f)
        {
            ResetPosition();
        }
    }

    private void FixedUpdate()
    {
       if (gameStarted) transform.Translate(0, upSpeed, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Slingshot"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            if (rb != null && rb.velocity.magnitude > 1f)
            {
                GameManager.Instance.AddScore(10);
                audioManager.PlaySFX(audioManager.ballonPop);

                DropRandomBalloonPrefab(); 

                ResetPosition();
            }
        }
    }

    private void DropRandomBalloonPrefab()
    {
        if (balloonDropPrefabs.Length == 0) return;

        int index = Random.Range(0, balloonDropPrefabs.Length);
        GameObject dropPrefab = balloonDropPrefabs[index];

        GameObject drop = Instantiate(dropPrefab, transform.position, Quaternion.identity);

        if (drop.name.Contains("House 1"))
        {
            audioManager.PlaySFX(audioManager.house);
        }
        else if (drop.name.Contains("Map"))
        {
            audioManager.PlaySFX(audioManager.map);
        }
        else if (drop.name.Contains("Dice"))
        {
            audioManager.PlaySFX(audioManager.dice);
        }
        else if (drop.name.Contains("Vase"))
        {
            audioManager.PlaySFX(audioManager.vase);
        }
        else if (drop.name.Contains("Bag"))
        {
            audioManager.PlaySFX(audioManager.bag);
        }

        Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.velocity = Vector2.zero;
        }
    }
    private void ResetPosition()
    {
        float safeMargin = 0.5f; 
        float screenHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float randomX = Random.Range(-screenHalfWidth + safeMargin, screenHalfWidth - safeMargin);
        float posY = -7f; 

        if (gameObject.name.Contains("RedBallon")) posY = -7f;
        
        else if (gameObject.name.Contains("YellowBallon")) posY = -9f;
        
        else if (gameObject.name.Contains("PinkBallon")) posY = -11f;
        

        transform.position = new Vector2(randomX, posY);
    }
}
