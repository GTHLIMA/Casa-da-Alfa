using UnityEngine;

public class ImageFusion : MonoBehaviour
{
    public DragManager manager;
    public GameObject currentTarget;
    private float fallLimitY;

    void Start()
    {
        if (currentTarget != null)
            fallLimitY = currentTarget.transform.position.y - 0.5f;
    }

    void Update()
    {
        if (transform.position.y < fallLimitY)
        {
            if (manager != null)
            {
                var logger = FindObjectOfType<DragGameLogger>();
                logger?.LogError("target", GetComponent<SpriteRenderer>().sprite.name, "object_fell");
                
                manager.RespawnAfterFall(gameObject);
                manager = null;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == currentTarget)
        {
            var playerSprite = GetComponent<SpriteRenderer>().sprite;
            var targetSprite = currentTarget.GetComponent<SpriteRenderer>().sprite; 

            var logger = FindObjectOfType<DragGameLogger>();

            if (playerSprite == targetSprite)
            {
                logger?.LogCorrectMatch(playerSprite.name, targetSprite.name);
                manager.HandleFusion();
            }
            else
            {
                logger?.LogError(targetSprite.name, playerSprite.name, "wrong_match");
            }
        }
    }
}