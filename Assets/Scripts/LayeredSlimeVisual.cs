using UnityEngine;
using System.Collections.Generic;

public class DynamicLayerVisual : MonoBehaviour
{
    [Header("Цвета слоёв")]
    public Color innerColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    public Color middleColor = new Color(0.3f, 1f, 0.3f, 0.7f);
    public Color outerColor = new Color(0.3f, 0.3f, 1f, 0.5f);

    [Header("Толщина линий")]
    public float innerWidth = 0.08f;
    public float middleWidth = 0.12f;
    public float outerWidth = 0.15f;

    private DynamicLayeredSlimeController controller;
    private LineRenderer innerRenderer, middleRenderer, outerRenderer;

    void Start()
    {
        controller = GetComponent<DynamicLayeredSlimeController>();
        if (controller == null) return;

        CreateRenderers();
    }

    void CreateRenderers()
    {
        CreateSingleRenderer(ref innerRenderer, "InnerRenderer", innerColor, innerWidth, controller.innerLayer.Count);
        CreateSingleRenderer(ref middleRenderer, "MiddleRenderer", middleColor, middleWidth, controller.middleLayer.Count);
        CreateSingleRenderer(ref outerRenderer, "OuterRenderer", outerColor, outerWidth, controller.outerLayer.Count);
    }

    void CreateSingleRenderer(ref LineRenderer renderer, string name, Color color, float width, int pointCount)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        renderer = obj.AddComponent<LineRenderer>();

        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.loop = true;
        renderer.useWorldSpace = true;
        renderer.positionCount = pointCount;
    }

    void Update()
    {
        if (controller == null) return;

        UpdateRendererPositions(innerRenderer, controller.innerLayer);
        UpdateRendererPositions(middleRenderer, controller.middleLayer);
        UpdateRendererPositions(outerRenderer, controller.outerLayer);
    }

    void UpdateRendererPositions(LineRenderer renderer, List<Rigidbody2D> layer)
    {
        if (layer == null || layer.Count == 0 || renderer == null) return;

        for (int i = 0; i < layer.Count && i < renderer.positionCount; i++)
        {
            if (layer[i] != null)
                renderer.SetPosition(i, layer[i].position);
        }
    }
}