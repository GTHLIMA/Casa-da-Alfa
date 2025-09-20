using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonFloatEffect : MonoBehaviour
{
    [Header("Configurações da Flutuação")]
    public float floatSpeed = 0.7f;
    public float floatHeight = 10f;

    private RectTransform animatedTarget;
    private Vector3 startLocalPos;

    void Awake()
    {
        EnsureAnimatedContainer();
        if (animatedTarget != null) startLocalPos = animatedTarget.localPosition;
    }

    void Update()
    {
        if (animatedTarget == null) return;
        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        animatedTarget.localPosition = startLocalPos + new Vector3(0f, offsetY, 0f);
    }

    private void EnsureAnimatedContainer()
    {
        if (animatedTarget != null) return;

        // captura filhos existentes antes de criar o container
        List<Transform> existingChildren = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            existingChildren.Add(transform.GetChild(i));

        // procura container já existente
        Transform found = transform.Find("__FloatContainer");
        if (found != null)
        {
            animatedTarget = found as RectTransform;
            return;
        }

        // cria container que preencherá a célula
        GameObject container = new GameObject("__FloatContainer", typeof(RectTransform));
        RectTransform cr = container.GetComponent<RectTransform>();
        cr.SetParent(transform, false);
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        // move os filhos visuais para dentro do container
        foreach (var child in existingChildren)
        {
            // o container ainda não estava na lista 'existingChildren', então movemos tudo
            child.SetParent(cr, false);
        }

        // se o objeto pai (o botão) tiver uma Image com sprite, copia para o container e desativa a principal
        Image parentImage = GetComponent<Image>();
        if (parentImage != null && parentImage.sprite != null)
        {
            Image childImage = container.AddComponent<Image>();
            childImage.sprite = parentImage.sprite;
            childImage.type = parentImage.type;
            childImage.preserveAspect = parentImage.preserveAspect;
            RectTransform ir = childImage.rectTransform;
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = Vector2.zero;
            ir.offsetMax = Vector2.zero;

            // esconde a imagem do pai (mantemos o componente para não quebrar referências de layout)
            parentImage.enabled = false;
        }

        animatedTarget = cr;
    }

    void OnDestroy()
    {
        // tenta restaurar posição do container (segurança)
        if (animatedTarget != null) animatedTarget.localPosition = Vector3.zero;
    }
}
