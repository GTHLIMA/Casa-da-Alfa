using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Rigidbody2D rb;
    private Slingshot slingshot;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        slingshot = FindObjectOfType<Slingshot>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Balloon"))
        {
            CallResetFromSlingshot();
        }
    }


    public void CallResetFromSlingshot()
    {

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