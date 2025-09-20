using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    [Header("Configurações da Flutuação")]
    [Tooltip("A velocidade do movimento de sobe e desce.")]
    public float floatSpeed = 1f;

    [Tooltip("A altura máxima que o objeto alcançará a partir de sua posição inicial.")]
    public float floatHeight = 20f;

    // Variáveis privadas para guardar a posição inicial
    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        // Guarda a referência ao RectTransform no início
        rectTransform = GetComponent<RectTransform>();
        // GUARDA A POSIÇÃO INICIAL. Isso é crucial para que o objeto flutue
        // em torno do seu ponto de partida, e não saia voando.
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // A mágica acontece aqui.
        // Mathf.Sin(Time.time * floatSpeed) cria um valor que oscila suavemente entre -1 e 1.
        // Multiplicamos pela altura (floatHeight) para controlar a amplitude do movimento.
        float newY = startPosition.y + (Mathf.Sin(Time.time * floatSpeed) * floatHeight);

        // Aplicamos a nova posição Y, mantendo a posição X original.
        rectTransform.anchoredPosition = new Vector2(startPosition.x, newY);
    }
}