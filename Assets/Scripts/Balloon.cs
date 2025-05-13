using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public float upSpeed; 
    AudioManager audioManager;

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Update()
    {
        if (transform.position.y > 7f)
        {
            ResetPosition();
        }

        
    }

    private void FixedUpdate()
    {
        transform.Translate(0, upSpeed, 0);
        
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
                ResetPosition();
            }
        }
    }

    private void ResetPosition()
    {
        float randomX = Random.Range(-2.5f, 2.5f);
        transform.position = new Vector2(randomX, -7f);

    }


}
