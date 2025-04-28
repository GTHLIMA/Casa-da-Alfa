using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{

    private bool hasScored = false;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                Collider2D hit = Physics2D.OverlapPoint(touchPosition);
                if (hit != null && hit.transform == transform)
                {
                    Destroy(gameObject);
                }
            }

        }
    }
    private IEnumerator DestroyAfterFrame()
    {
        yield return null; // espera um frame
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasScored) return; // Evita múltiplas pontuações
        if (other.CompareTag("Ground"))
        {
            hasScored = true;
            GameManager.Instance.AddScore(1);
            StartCoroutine(DestroyAfterFrame());
        }   
    }

}