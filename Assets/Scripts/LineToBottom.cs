using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineToBottom : MonoBehaviour
{
    public float lineHeight = 50f; // QUÃO LONGA A LINHA É PARA BAIXO
    public float verticalOffset = 0.5f; // DISTÂNCIA DO PREFAB PARA O INÍCIO DA LINHA
    public float lineWidth = 0.05f; // LARGURA VISUAL DO TRAÇO
    public LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // Define largura visual (espessura)
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

    }

    void Update()
    {
        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        Vector3 start = transform.position + Vector3.down * verticalOffset;
        Vector3 end = start + Vector3.down * lineHeight;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        float textureTiling = lineHeight / lineWidth; 
        if (lineRenderer.material != null)
        {
            lineRenderer.material.mainTextureScale = new Vector2(1f, textureTiling);
        }
    }
}
