using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SlotFloatEffect : MonoBehaviour
{
    [Header("Configurações da Flutuação")]
    public float floatSpeed = 2f;
    public float floatHeight = 10f;
    public float duration = 1.5f;

    private RectTransform animatedTarget;
    private Vector3 startLocalPos;
    private float timer = 0f;

    void Awake()
    {
        EnsureAnimatedContainer();
        if (animatedTarget != null) startLocalPos = animatedTarget.localPosition;
    }

    void Update()
    {
        if (animatedTarget == null) return;

        if (timer < duration)
        {
            timer += Time.deltaTime;
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            animatedTarget.localPosition = startLocalPos + new Vector3(0f, offsetY, 0f);
        }
        else
        {
            // reseta e destrói o componente
            animatedTarget.localPosition = startLocalPos;
            Destroy(this);
        }
    }

    private void EnsureAnimatedContainer()
    {
        if (animatedTarget != null) return;

        // captura filhos existentes
        List<Transform> existingChildren = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            existingChildren.Add(transform.GetChild(i));

        // se já existe container, usa ele
        Transform found = transform.Find("__FloatContainer");
        if (found != null)
        {
            animatedTarget = found as RectTransform;
            return;
        }

        // cria container
        GameObject container = new GameObject("__FloatContainer", typeof(RectTransform));
        RectTransform cr = container.GetComponent<RectTransform>();
        cr.SetParent(transform, false);
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        // move filhos visuais para dentro do container
        foreach (var child in existingChildren)
        {
            child.SetParent(cr, false);
        }

        // copia imagem do pai para o container (se houver) e desativa a imagem do pai
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

            parentImage.enabled = false;
        }

        animatedTarget = cr;
    }
}
