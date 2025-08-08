using UnityEngine;

public class ImageFusion : MonoBehaviour
{
    public DragManager manager;
    public GameObject currentTarget;

    private float fallLimitY;
    private bool hasFallen = false;

    void Start()
    {
        UpdateFallLimit();
    }

    void Update()
    {
        if (!hasFallen && transform.position.y < fallLimitY)
        {
            hasFallen = true;
            if (manager != null)
            {
                manager.RespawnAfterFall(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == currentTarget)
        {
            var playerSprite = GetComponent<SpriteRenderer>().sprite;
            var targetSprite = currentTarget.GetComponent<SpriteRenderer>().sprite;

            if (playerSprite == targetSprite)
            {
                manager.HandleFusion(); 
            }
            else
            {
                Debug.Log("Sprites diferentes: fusão cancelada.");
            }
        }
    }

    public void UpdateFallLimit()
    {
        if (currentTarget != null)
            fallLimitY = currentTarget.transform.position.y - 0.5f;
    }
}
