using UnityEngine;

public class ImageFusion : MonoBehaviour
{
    public DragManager manager;
    public GameObject currentTarget;
    private float fallLimitY;

    void Start()
    {
        // Define o limite com base na posição Y do target (para cair abaixo)
        if (currentTarget != null)
            fallLimitY = currentTarget.transform.position.y - 0.5f; // margem de tolerância
    }

    void Update()
    {
        if (transform.position.y < fallLimitY)
        {
            // Evita múltiplas chamadas
            if (manager != null)
            {
                manager.RespawnAfterFall(gameObject);
                manager = null; // Impede chamadas duplicadas
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Confirma que encostou no target
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

}
