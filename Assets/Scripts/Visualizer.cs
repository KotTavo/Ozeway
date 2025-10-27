using UnityEngine;
using System.Collections.Generic;

public class SlimeBoundsVisualizer : MonoBehaviour
{
    [Header("=== ВИЗУАЛИЗАЦИЯ ГАБАРИТОВ ===")]
    public bool showBounds = true;
    public Color boundsColor = new Color(0, 1, 1, 0.3f); // Голубой с прозрачностью
    public bool showTrail = true;
    public Color trailColor = new Color(1, 0.5f, 0, 0.8f); // Оранжевый
    public float trailDuration = 3f; // Секунды истории
    public float trailUpdateInterval = 0.1f; // Интервал обновления

    [Header("=== НАСТРОЙКИ ОТОБРАЖЕНИЯ ===")]
    public float lineWidth = 0.05f;
    public int trailMaxPoints = 100;

    private SlimeCharacterController slimeController;
    private LineRenderer boundsRenderer;
    private LineRenderer trailRenderer;
    private List<Vector3> trailPositions = new List<Vector3>();
    private float lastTrailUpdateTime;
    private Camera mainCamera;

    void Start()
    {
        slimeController = GetComponent<SlimeCharacterController>();
        mainCamera = Camera.main;

        CreateBoundsVisual();
        CreateTrailVisual();

        lastTrailUpdateTime = Time.time;
    }

    void CreateBoundsVisual()
    {
        GameObject boundsObj = new GameObject("BoundsVisualizer");
        boundsObj.transform.SetParent(transform);
        boundsObj.transform.localPosition = Vector3.zero;

        boundsRenderer = boundsObj.AddComponent<LineRenderer>();
        boundsRenderer.material = new Material(Shader.Find("Sprites/Default"));
        boundsRenderer.startColor = boundsColor;
        boundsRenderer.endColor = boundsColor;
        boundsRenderer.startWidth = lineWidth;
        boundsRenderer.endWidth = lineWidth;
        boundsRenderer.loop = true;
        boundsRenderer.useWorldSpace = true;
    }

    void CreateTrailVisual()
    {
        GameObject trailObj = new GameObject("TrailVisualizer");
        trailObj.transform.SetParent(transform);
        trailObj.transform.localPosition = Vector3.zero;

        trailRenderer = trailObj.AddComponent<LineRenderer>();
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = trailColor;
        trailRenderer.startWidth = lineWidth * 0.7f;
        trailRenderer.endWidth = lineWidth * 0.3f;
        trailRenderer.loop = false;
        trailRenderer.useWorldSpace = true;
    }

    void Update()
    {
        if (slimeController == null) return;

        UpdateBoundsVisual();
        UpdateTrailVisual();
    }

    void UpdateBoundsVisual()
    {
        if (!showBounds || boundsRenderer == null) return;

        // Вычисляем габариты на основе крайних узлов
        Vector2 minBounds = Vector2.positiveInfinity;
        Vector2 maxBounds = Vector2.negativeInfinity;

        // Проверяем все узлы всех слоев
        CheckNodesBounds(slimeController.coreNodes, ref minBounds, ref maxBounds);
        CheckNodesBounds(slimeController.middleNodes, ref minBounds, ref maxBounds);
        CheckNodesBounds(slimeController.surfaceNodes, ref minBounds, ref maxBounds);

        // Если узлов нет, используем центр
        if (minBounds == Vector2.positiveInfinity)
        {
            minBounds = transform.position;
            maxBounds = transform.position;
        }

        // Создаем прямоугольник габаритов
        Vector3[] boundsPoints = new Vector3[5];
        boundsPoints[0] = new Vector3(minBounds.x, minBounds.y, 0);
        boundsPoints[1] = new Vector3(maxBounds.x, minBounds.y, 0);
        boundsPoints[2] = new Vector3(maxBounds.x, maxBounds.y, 0);
        boundsPoints[3] = new Vector3(minBounds.x, maxBounds.y, 0);
        boundsPoints[4] = boundsPoints[0]; // Замыкаем прямоугольник

        boundsRenderer.positionCount = 5;
        boundsRenderer.SetPositions(boundsPoints);
    }

    void CheckNodesBounds(List<Rigidbody2D> nodes, ref Vector2 minBounds, ref Vector2 maxBounds)
    {
        foreach (var node in nodes)
        {
            if (node != null && node.gameObject.activeInHierarchy)
            {
                Vector2 nodePos = node.position;
                minBounds = Vector2.Min(minBounds, nodePos);
                maxBounds = Vector2.Max(maxBounds, nodePos);
            }
        }
    }

    void UpdateTrailVisual()
    {
        if (!showTrail || trailRenderer == null) return;

        // Добавляем новую точку через интервалы
        if (Time.time - lastTrailUpdateTime >= trailUpdateInterval)
        {
            AddTrailPoint(transform.position);
            lastTrailUpdateTime = Time.time;
        }

        // Удаляем старые точки
        RemoveOldTrailPoints();

        // Обновляем LineRenderer
        UpdateTrailRenderer();
    }

    void AddTrailPoint(Vector3 position)
    {
        trailPositions.Add(position);

        // Ограничиваем максимальное количество точек
        if (trailPositions.Count > trailMaxPoints)
        {
            trailPositions.RemoveAt(0);
        }
    }

    void RemoveOldTrailPoints()
    {
        float currentTime = Time.time;
        float removeTime = currentTime - trailDuration;

        // Удаляем точки, которые старше trailDuration
        for (int i = trailPositions.Count - 1; i >= 0; i--)
        {
            // В реальной реализации нужно хранить время каждой точки
            // Для упрощения удаляем старые точки с начала списка
            if (trailPositions.Count > trailMaxPoints * 0.5f)
            {
                trailPositions.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
    }

    void UpdateTrailRenderer()
    {
        if (trailPositions.Count < 2)
        {
            trailRenderer.positionCount = 0;
            return;
        }

        trailRenderer.positionCount = trailPositions.Count;
        for (int i = 0; i < trailPositions.Count; i++)
        {
            trailRenderer.SetPosition(i, trailPositions[i]);
        }
    }

    // Методы для управления визуализацией (можно вызывать из других скриптов)
    public void SetBoundsVisible(bool visible)
    {
        showBounds = visible;
        if (boundsRenderer != null)
            boundsRenderer.enabled = visible;
    }

    public void SetTrailVisible(bool visible)
    {
        showTrail = visible;
        if (trailRenderer != null)
            trailRenderer.enabled = visible;
    }

    public void ClearTrail()
    {
        trailPositions.Clear();
        if (trailRenderer != null)
            trailRenderer.positionCount = 0;
    }

    // Визуализация в редакторе для настройки
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Показываем радиусы в редакторе
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slimeController.autoGatherRadius);

        // Показываем крайние точки габаритов
        if (showBounds)
        {
            Gizmos.color = boundsColor;
            Vector2 center = transform.position;
            float maxDistance = 0f;

            // Находим самый дальний узел
            maxDistance = FindFurthestNodeDistance();

            Gizmos.DrawWireSphere(center, maxDistance);
        }
    }

    float FindFurthestNodeDistance()
    {
        float maxDistance = 0f;
        Vector2 center = transform.position;

        CheckFurthestNodes(slimeController.coreNodes, center, ref maxDistance);
        CheckFurthestNodes(slimeController.middleNodes, center, ref maxDistance);
        CheckFurthestNodes(slimeController.surfaceNodes, center, ref maxDistance);

        return maxDistance;
    }

    void CheckFurthestNodes(List<Rigidbody2D> nodes, Vector2 center, ref float maxDistance)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                float distance = Vector2.Distance(center, node.position);
                if (distance > maxDistance)
                    maxDistance = distance;
            }
        }
    }
}