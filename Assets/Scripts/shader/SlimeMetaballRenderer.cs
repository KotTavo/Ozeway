using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SlimeCharacterController))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SlimeMetaballRenderer : MonoBehaviour
{
    [Header("Настройки метабол")]
    [Tooltip("Радиус влияния для узлов ядра")]
    public float coreRadius = 1.2f;
    [Tooltip("Радиус влияния для средних узлов")]
    public float middleRadius = 1.0f;
    [Tooltip("Радиус влияния для внешних узлов")]
    public float surfaceRadius = 0.8f;
    [Tooltip("Максимальное расстояние между узлами контура. Если больше - создаются мнимые узлы для сглаживания.")]
    public float maxNodeDistanceForPhantom = 1.5f;

    [Header("Ссылки и преднастройки")]
    [Tooltip("Материал, использующий SlimeMetaballShader")]
    public Material slimeMaterial;

    private SlimeCharacterController slimeController;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh quadMesh;

    // Максимальное количество узлов, которое может обработать шейдер
    private const int MAX_NODES = 256;
    private Vector4[] nodePositionsArray = new Vector4[MAX_NODES];

    // Идентификаторы свойств шейдера для производительности
    private static readonly int NodePositionsID = Shader.PropertyToID("_NodePositions");
    private static readonly int NodeCountID = Shader.PropertyToID("_NodeCount");
    private static readonly int SlimeCenterID = Shader.PropertyToID("_SlimeCenter");
    private static readonly int MaxRadiusID = Shader.PropertyToID("_MaxRadius");

    void Start()
    {
        slimeController = GetComponent<SlimeCharacterController>();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material = slimeMaterial;

        CreateQuadMesh();
        meshFilter.mesh = quadMesh;
    }

    void LateUpdate()
    {
        if (slimeController == null || slimeMaterial == null) return;

        // 1. Подготовить все данные (включая мнимые узлы) для шейдера
        float maxDistFromCenter = 0f;
        int totalNodeCount = GenerateShaderNodeData(ref maxDistFromCenter);

        if (totalNodeCount == 0)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.enabled = true;

        // 2. Рассчитать габариты на основе реальных узлов и обновить "холст"
        Bounds bounds = CalculateRealNodeBounds();
        UpdateQuadMesh(bounds);

        // 3. Отправить финальные данные в материал/шейдер
        slimeMaterial.SetVectorArray(NodePositionsID, nodePositionsArray);
        slimeMaterial.SetInt(NodeCountID, totalNodeCount);
        slimeMaterial.SetVector(SlimeCenterID, transform.position);
        slimeMaterial.SetFloat(MaxRadiusID, maxDistFromCenter > 0.01f ? maxDistFromCenter : 1f);
    }

    /// <summary>
    /// Собирает все реальные узлы, создает мнимые узлы в разрывах контура
    /// и подготавливает финальный массив данных для отправки в шейдер.
    /// </summary>
    /// <returns>Общее количество узлов (реальных + мнимых) для шейдера.</returns>
    private int GenerateShaderNodeData(ref float maxDistFromCenter)
    {
        var shaderNodes = new List<Vector4>(MAX_NODES);
        Vector2 center = transform.position;

        // --- 1. Добавляем узлы ядра и середины (они не нуждаются в сглаживании) ---
        foreach (var node in slimeController.coreNodes)
        {
            if (node != null) shaderNodes.Add(new Vector4(node.position.x, node.position.y, 0, coreRadius));
        }
        foreach (var node in slimeController.middleNodes)
        {
            if (node != null) shaderNodes.Add(new Vector4(node.position.x, node.position.y, 0, middleRadius));
        }

        // --- 2. Обрабатываем узлы контура и создаем мнимые узлы в разрывах ---
        var surfaceNodes = slimeController.surfaceNodes;
        if (surfaceNodes.Count >= 2)
        {
            for (int i = 0; i < surfaceNodes.Count; i++)
            {
                Rigidbody2D currentNode = surfaceNodes[i];
                if (currentNode == null) continue;

                // Всегда добавляем текущий реальный узел
                shaderNodes.Add(new Vector4(currentNode.position.x, currentNode.position.y, 0, surfaceRadius));

                // "Зацикливаем" список, чтобы последний узел соединялся с первым
                Rigidbody2D nextNode = surfaceNodes[(i + 1) % surfaceNodes.Count];
                if (nextNode == null) continue;

                float dist = Vector2.Distance(currentNode.position, nextNode.position);

                // Если расстояние слишком большое, генерируем мнимые узлы между текущим и следующим
                if (dist > maxNodeDistanceForPhantom)
                {
                    int phantomCount = Mathf.FloorToInt(dist / maxNodeDistanceForPhantom);
                    for (int j = 1; j <= phantomCount; j++)
                    {
                        float t = (float)j / (phantomCount + 1);
                        Vector2 phantomPos = Vector2.Lerp(currentNode.position, nextNode.position, t);
                        // Мнимые узлы имеют тот же радиус, что и узлы контура
                        shaderNodes.Add(new Vector4(phantomPos.x, phantomPos.y, 0, surfaceRadius));
                    }
                }
            }
        }
        else // Если узлов контура слишком мало для анализа, просто добавляем их
        {
            foreach (var node in surfaceNodes)
            {
                if (node != null) shaderNodes.Add(new Vector4(node.position.x, node.position.y, 0, surfaceRadius));
            }
        }

        // --- 3. Копируем финальный список в массив для шейдера и вычисляем максимальное расстояние от центра ---
        maxDistFromCenter = 0f;
        for (int i = 0; i < shaderNodes.Count; i++)
        {
            if (i >= MAX_NODES) break; // Защита от переполнения массива

            nodePositionsArray[i] = shaderNodes[i];

            float dist = Vector2.Distance(new Vector2(shaderNodes[i].x, shaderNodes[i].y), center);
            if (dist > maxDistFromCenter)
            {
                maxDistFromCenter = dist;
            }
        }

        return shaderNodes.Count;
    }

    /// <summary>
    /// Вычисляет ограничивающую рамку (Bounds) только на основе реальных узлов.
    /// </summary>
    private Bounds CalculateRealNodeBounds()
    {
        var realNodes = new List<Rigidbody2D>();

        AddValidNodesToList(slimeController.coreNodes, realNodes);
        AddValidNodesToList(slimeController.middleNodes, realNodes);
        AddValidNodesToList(slimeController.surfaceNodes, realNodes);

        if (realNodes.Count == 0) return new Bounds(transform.position, Vector3.one);

        // Инициализируем рамку позицией первого узла
        var bounds = new Bounds(realNodes[0].position, Vector3.zero);
        // Расширяем рамку, чтобы она включала все остальные узлы
        for (int i = 1; i < realNodes.Count; i++)
        {
            bounds.Encapsulate(realNodes[i].position);
        }

        // Добавляем отступ, чтобы края слизи, отрисованные шейдером, не обрезались
        float padding = Mathf.Max(coreRadius, middleRadius, surfaceRadius);
        bounds.Expand(padding * 2);
        return bounds;
    }

    /// <summary>
    /// Вспомогательный метод для добавления не-пустых узлов в список.
    /// </summary>
    private void AddValidNodesToList(List<Rigidbody2D> source, List<Rigidbody2D> destination)
    {
        foreach (var node in source)
        {
            if (node != null)
            {
                destination.Add(node);
            }
        }
    }

    /// <summary>
    /// Создает базовый квадратный меш ("холст").
    /// </summary>
    private void CreateQuadMesh()
    {
        quadMesh = new Mesh
        {
            name = "SlimeCanvas",
            vertices = new Vector3[4],
            triangles = new int[] { 0, 1, 2, 2, 3, 0 },
            uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }
        };
        quadMesh.MarkDynamic();
    }

    /// <summary>
    /// Обновляет вершины "холста", чтобы он всегда покрывал всю область слизи.
    /// </summary>
    private void UpdateQuadMesh(Bounds bounds)
    {
        Vector3 parentPosition = transform.position;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        Vector3[] vertices = new Vector3[4];
        // Вычисляем мировые координаты углов, а затем преобразуем их в локальные
        vertices[0] = new Vector3(center.x - size.x / 2, center.y - size.y / 2, 0) - parentPosition;
        vertices[1] = new Vector3(center.x - size.x / 2, center.y + size.y / 2, 0) - parentPosition;
        vertices[2] = new Vector3(center.x + size.x / 2, center.y + size.y / 2, 0) - parentPosition;
        vertices[3] = new Vector3(center.x + size.x / 2, center.y - size.y / 2, 0) - parentPosition;

        quadMesh.vertices = vertices;
        quadMesh.RecalculateBounds();
    }
}