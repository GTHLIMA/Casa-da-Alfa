using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    [Header("Configurações da Flutuação")]

    [Tooltip("A velocidade do movimento de sobe e desce.")]
    public float speed = 1f;

    [Tooltip("A altura máxima que o objeto vai subir e descer a partir do seu ponto inicial.")]
    public float amplitude = 0.2f;

    // Guarda a posição inicial do objeto para que a flutuação seja sempre em relação a ela.
    private Vector3 startPosition;

    void Start()
    {
        // No início, salvamos a posição original do objeto.
        startPosition = transform.position;
    }

    void Update()
    {
        // Calcula a nova posição Y usando uma onda de seno.
        // Time.time faz com que o movimento seja contínuo e baseado no tempo de jogo.
        float newY = startPosition.y + Mathf.Sin(Time.time * speed) * amplitude;

        // Aplica a nova posição Y, mantendo o X e o Z originais.
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
}