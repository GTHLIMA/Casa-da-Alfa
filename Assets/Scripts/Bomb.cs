using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{

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

    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Ground"))
    {
        Destroy(gameObject);
    }   
}

}