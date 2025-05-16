using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GroundWidth : MonoBehaviour
{    
    public BoxCollider2D groundCollider;

    void Start()
    {
        groundCollider = GetComponent<BoxCollider2D>();
        float screenHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;

        Vector2 newSize = groundCollider.size;
        newSize.x = screenHalfWidth * 2f;
        groundCollider.size = newSize;


    }
}
