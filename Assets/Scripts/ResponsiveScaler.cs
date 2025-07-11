using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ResponsiveScaler : MonoBehaviour
{
    public float preferredWidthPercent = 0.15f; // Largura ocupada na tela (15% da largura)
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        ScaleSpriteToScreen();
    }

    void ScaleSpriteToScreen()
    {
        // Largura visível da tela (em unidades do mundo)
        float screenWorldWidth = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 10)).x -
                                 Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 10)).x;

        // Largura alvo do sprite no mundo (15% da largura da tela)
        float targetWorldWidth = screenWorldWidth * preferredWidthPercent;

        // Tamanho original do sprite
        float spriteWorldWidth = sr.bounds.size.x;

        // Fator de escala necessário
        float scale = targetWorldWidth / spriteWorldWidth;

        // Aplicar a escala proporcional
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
