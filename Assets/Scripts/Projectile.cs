using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Rigidbody2D rb;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CallResetFromSlingshot();
    }

    private void OnBecameInvisible()
    {
        CallResetFromSlingshot();
    }

    void CallResetFromSlingshot()
    {
        Slingshot slingshot = FindObjectOfType<Slingshot>();

        if (slingshot != null)
        {
            slingshot.ResetProjectile();
        }
        else
        {
            Debug.LogError("Slingshot não encontrado na cena!");
        }
    }
}


