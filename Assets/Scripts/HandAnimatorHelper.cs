using UnityEngine;

public class HandAnimatorHelper : MonoBehaviour
{
    // Ajuste este valor para corresponder EXATAMENTE
    // à duração da sua animação de mão em segundos.
    public float animationDuration = 0.5f;

    void Start()
    {
        // Garante que a mão apareça na frente
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Effects"; // Use a Sorting Layer que fica na frente
            sr.sortingOrder = 10; // Use uma ordem alta se necessário
        }

        // Destroi este objeto (a mão) após a duração da animação
        Destroy(gameObject, animationDuration);
    }


}
