using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DynamicLayeredSlimeController : MonoBehaviour
{
    [Header("=== ��������� ��Ψ� ===")]
    public int innerPoints = 12;
    public int middlePoints = 16;
    public int outerPoints = 24;
    public float[] layerRadii = new float[] { 0.8f, 1.3f, 1.8f };

    [Header("=== ������ ��Ψ� ===")]
    public float innerLayerStiffness = 1500f;
    public float middleLayerStiffness = 400f;
    public float outerLayerStiffness = 80f;

    [Header("=== ����������� ���������� ===")]
    public float maxMiddleStretch = 1.5f;
    public float maxOuterStretch = 2.0f;
    public float stretchRecoveryForce = 50f;

    [Header("=== ������������ �������� ===")]
    public float collisionCheckRadius = 0.3f;
    public float layerRepulsionForce = 100f;
    public float minDistanceBetweenLayers = 0.1f;

    [Header("=== ���������� ===")]
    public float movementSpeed = 8f;
    public float jumpForce = 12f;
    public float gravityScale = 3f;

    [Header("=== ��������� ��Ψ� ===")]
    public float innerLayerPriority = 2.0f;
    public float middleLayerPriority = 1.2f;
    public float outerLayerPriority = 0.8f;

    [Header("=== ��������� ���������� ===")]
    public float precisionMultiplier = 1.5f;
    public float weightCompensation = 2f;
    public float inputResponseCurve = 2.0f;

    [Header("=== ���������������� �������� ===")]
    public float adaptiveCollisionRadius = 0.4f;
    public float layerResponseCurve = 1.5f;
    public float predictiveCollisionForce = 80f;

    [Header("=== ������������ ��������� ===")]
    public float currentMassMultiplier = 1f;
    public float currentSizeMultiplier = 1f;
    public Color currentColor = Color.white;

    [Header("=== ����������� ===")]
    public bool useSimpleJoints = true;
    public bool enablePerformanceMonitoring = false;

    public List<Rigidbody2D> innerLayer { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> middleLayer { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> outerLayer { get; private set; } = new List<Rigidbody2D>();

    private Rigidbody2D centerRigidbody;
    private bool isGrounded = false;
    private Vector2 movementInput;
    private Vector2[] previousInnerPositions;

    // ��� ������������ ��������
    private List<Vector2> innerLayerNormals = new List<Vector2>();
    private List<Vector2> middleLayerNormals = new List<Vector2>();

    // ������������������
    private int physicsUpdatesPerSecond = 0;
    private float lastPerformanceCheck;

    void Start()
    {
        CreateTagIfNeeded("SlimeNode");
        CreateTagIfNeeded("Ground");
        InitializeLayers();
        GenerateCollisionNormals();

        // ������������� ��� ������������ ��������
        previousInnerPositions = new Vector2[innerLayer.Count];
        for (int i = 0; i < innerLayer.Count; i++)
        {
            previousInnerPositions[i] = innerLayer[i].position;
        }
    }

    void CreateTagIfNeeded(string tagName)
    {
        if (!TagExists(tagName))
        {
#if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.AddTag(tagName);
#endif
        }
    }

    bool TagExists(string tagName)
    {
        try
        {
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    void InitializeLayers()
    {
        CreateCenterPoint();
        CreateLayers();
        ConnectLayersOptimized();
    }

    void CreateCenterPoint()
    {
        centerRigidbody = GetComponent<Rigidbody2D>();
        if (centerRigidbody == null)
            centerRigidbody = gameObject.AddComponent<Rigidbody2D>();

        centerRigidbody.gravityScale = 0;
        centerRigidbody.linearDamping = 0.3f;
        centerRigidbody.angularDamping = 0.5f;
        centerRigidbody.mass = 1f;
        centerRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void CreateLayers()
    {
        CreateLayer(0, "Inner", innerLayer, innerPoints, 1.0f, 0.8f, 0.08f);
        CreateLayer(1, "Middle", middleLayer, middlePoints, 0.6f, 1.2f, 0.1f);
        CreateLayer(2, "Outer", outerLayer, outerPoints, 0.3f, 1.5f, 0.12f);
    }

    void CreateLayer(int layerIndex, string layerName, List<Rigidbody2D> layer,
        int pointCount, float drag, float gravityMultiplier, float colliderSize)
    {
        float radius = layerRadii[layerIndex];
        for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
        {
            float angle = pointIndex * (360f / pointCount);
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;
            Vector2 position = (Vector2)transform.position + direction * radius;

            GameObject point = new GameObject($"{layerName}_Point_{pointIndex}");
            point.transform.position = position;
            point.transform.SetParent(transform);
            point.tag = "SlimeNode";

            Rigidbody2D rb = point.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale * gravityMultiplier;
            rb.linearDamping = drag;
            rb.angularDamping = drag * 1.5f;
            rb.mass = (0.2f + (layerIndex * 0.15f)) * currentMassMultiplier;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = point.AddComponent<CircleCollider2D>();
            collider.radius = colliderSize;

            layer.Add(rb);
        }
    }

    void GenerateCollisionNormals()
    {
        // ���������� ������� ��� ����������� ����
        for (int i = 0; i < innerPoints; i++)
        {
            float angle = i * (360f / innerPoints);
            Vector2 normal = Quaternion.Euler(0, 0, angle) * Vector2.up;
            innerLayerNormals.Add(normal);
        }

        // ���������� ������� ��� �������� ����
        for (int i = 0; i < middlePoints; i++)
        {
            float angle = i * (360f / middlePoints);
            Vector2 normal = Quaternion.Euler(0, 0, angle) * Vector2.up;
            middleLayerNormals.Add(normal);
        }
    }

    void ConnectLayersOptimized()
    {
        ConnectLayerToCenter(innerLayer, innerPoints, 0, innerLayerStiffness, 0.9f);

        if (useSimpleJoints)
        {
            ConnectLayerToLayerSimple(middleLayer, innerLayer, layerRadii[1] - layerRadii[0], middleLayerStiffness);
            ConnectLayerToLayerSimple(outerLayer, middleLayer, layerRadii[2] - layerRadii[1], outerLayerStiffness);
        }
        else
        {
            ConnectLayerToPreviousLayer(middleLayer, innerLayer, middlePoints, innerPoints, 1, middleLayerStiffness, 0.7f);
            ConnectLayerToPreviousLayer(outerLayer, middleLayer, outerPoints, middlePoints, 2, outerLayerStiffness, 0.3f);
        }

        ConnectPointsWithinLayer(innerLayer, innerPoints, innerLayerStiffness * 0.8f, 0.8f);
        ConnectPointsWithinLayer(middleLayer, middlePoints, middleLayerStiffness * 0.6f, 0.5f);
        ConnectPointsWithinLayer(outerLayer, outerPoints, outerLayerStiffness * 0.4f, 0.2f);
    }

    void ConnectLayerToCenter(List<Rigidbody2D> layer, int pointCount, int layerIndex, float stiffness, float damping)
    {
        float radius = layerRadii[layerIndex];
        foreach (Rigidbody2D pointRB in layer)
        {
            DistanceJoint2D joint = pointRB.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = centerRigidbody;
            joint.distance = radius;
            joint.maxDistanceOnly = false;
            joint.autoConfigureDistance = false;
        }
    }

    void ConnectLayerToLayerSimple(List<Rigidbody2D> currentLayer, List<Rigidbody2D> previousLayer, float distance, float stiffness)
    {
        for (int i = 0; i < currentLayer.Count; i++)
        {
            if (currentLayer[i] == null) continue;

            int targetIndex = i % previousLayer.Count;
            if (previousLayer[targetIndex] == null) continue;

            SpringJoint2D joint = currentLayer[i].gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = previousLayer[targetIndex];
            joint.distance = distance;
            joint.dampingRatio = 0.7f;
            joint.frequency = stiffness / 150f;
            joint.autoConfigureConnectedAnchor = false;
        }
    }

    void ConnectLayerToPreviousLayer(List<Rigidbody2D> currentLayer, List<Rigidbody2D> previousLayer,
        int currentPoints, int previousPoints, int layerIndex, float stiffness, float damping)
    {
        float distanceBetweenLayers = layerRadii[layerIndex] - layerRadii[layerIndex - 1];
        for (int i = 0; i < currentPoints; i++)
        {
            int closestIndex = Mathf.RoundToInt((float)i / currentPoints * previousPoints) % previousPoints;

            SpringJoint2D joint = currentLayer[i].gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = previousLayer[closestIndex];
            joint.distance = distanceBetweenLayers;
            joint.autoConfigureDistance = false;
            joint.dampingRatio = damping;
            joint.frequency = stiffness / 100f;
        }
    }

    void ConnectPointsWithinLayer(List<Rigidbody2D> layer, int pointCount, float stiffness, float damping)
    {
        for (int i = 0; i < pointCount; i++)
        {
            int nextIndex = (i + 1) % pointCount;

            SpringJoint2D joint = layer[i].gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = layer[nextIndex];

            float layerRadius = GetLayerRadius(layer);
            float distance = 2f * Mathf.Sin(Mathf.PI / pointCount) * layerRadius;

            joint.distance = distance;
            joint.autoConfigureDistance = false;
            joint.dampingRatio = damping;
            joint.frequency = stiffness / 100f;
        }
    }

    float GetLayerRadius(List<Rigidbody2D> layer)
    {
        if (layer == innerLayer) return layerRadii[0];
        if (layer == middleLayer) return layerRadii[1];
        if (layer == outerLayer) return layerRadii[2];
        return 1f;
    }

    void Update()
    {
        HandleInput();

        // ���������������� ���������� ������ ��� ������������������
        if (Time.time % 0.02f < Time.deltaTime)
        {
            HandleHybridMovement();
            HandleJump();
            UpdateLayerSpecificPhysics();
            ApplyStretchLimitations();
            HandleOptimizedInterLayerCollisions();
        }

        if (enablePerformanceMonitoring)
            MonitorPerformance();
    }

    void HandleInput()
    {
        movementInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) movementInput.y += 1f;
        if (Input.GetKey(KeyCode.S)) movementInput.y -= 1f;
        if (Input.GetKey(KeyCode.A)) movementInput.x -= 1f;
        if (Input.GetKey(KeyCode.D)) movementInput.x += 1f;
        movementInput = movementInput.normalized;
    }

    void HandleHybridMovement()
    {
        Vector2 targetVelocity = movementInput * movementSpeed;

        // ������ ������� - ������� ��������� ������, �� � ��������
        float inputMagnitude = movementInput.magnitude;
        float responseMultiplier = Mathf.Pow(inputMagnitude, inputResponseCurve) * precisionMultiplier;

        // �������� ���� � ������
        Vector2 velocityChange = targetVelocity - centerRigidbody.linearVelocity;
        centerRigidbody.AddForce(velocityChange * 15f * responseMultiplier);

        // "���" - ����������� ������������ �������� ��� ������ ���������
        if (movementInput.magnitude > 0.1f)
        {
            float currentSpeed = centerRigidbody.linearVelocity.magnitude;
            float targetSpeed = movementSpeed * inputMagnitude;

            if (currentSpeed > targetSpeed)
            {
                centerRigidbody.linearVelocity = Vector2.ClampMagnitude(centerRigidbody.linearVelocity, targetSpeed);
            }
        }

        // ��������� ���������� ���� � �����
        ApplyCascadeForcesToLayers(targetVelocity, responseMultiplier);
    }

    void ApplyCascadeForcesToLayers(Vector2 targetVelocity, float responseMultiplier)
    {
        // ���������� ���� - ������ ����������
        foreach (Rigidbody2D rb in innerLayer)
        {
            if (rb == null) continue;
            Vector2 layerVelocityChange = targetVelocity * 0.8f - rb.linearVelocity;
            rb.AddForce(layerVelocityChange * 12f * responseMultiplier);
        }

        // ������� ���� - ���������� ����������
        foreach (Rigidbody2D rb in middleLayer)
        {
            if (rb == null) continue;
            Vector2 layerVelocityChange = targetVelocity * 0.5f - rb.linearVelocity;
            rb.AddForce(layerVelocityChange * 8f * responseMultiplier);
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            centerRigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            AddForceToLayer(innerLayer, jumpForce * 0.1f);
            AddForceToLayer(middleLayer, jumpForce * 0.2f);
            AddForceToLayer(outerLayer, jumpForce * 0.3f);
        }
    }

    void AddForceToLayer(List<Rigidbody2D> layer, float force)
    {
        foreach (Rigidbody2D pointRB in layer)
        {
            pointRB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        }
    }

    void UpdateLayerSpecificPhysics()
    {
        // ��������� ������������ ��������
        for (int i = 0; i < innerLayer.Count; i++)
        {
            if (innerLayer[i] == null) continue;
            previousInnerPositions[i] = innerLayer[i].position;
        }

        // ������������ ������ �����
        foreach (Rigidbody2D rb in innerLayer)
        {
            rb.gravityScale = gravityScale * 0.3f;
            rb.mass = 0.1f * currentMassMultiplier;
        }

        foreach (Rigidbody2D rb in middleLayer)
        {
            rb.gravityScale = gravityScale * 0.8f;
            rb.linearDamping = 1.2f;
            rb.mass = 0.2f * currentMassMultiplier;
        }

        foreach (Rigidbody2D rb in outerLayer)
        {
            rb.gravityScale = gravityScale * 1.5f;
            rb.linearDamping = 0.8f;
            rb.mass = 0.3f * currentMassMultiplier;
        }
    }

    void ApplyStretchLimitations()
    {
        // ������������ ���������� �������� ����
        foreach (Rigidbody2D point in middleLayer)
        {
            float currentDistance = Vector2.Distance(centerRigidbody.position, point.position);
            float maxAllowedDistance = layerRadii[1] * maxMiddleStretch;

            if (currentDistance > maxAllowedDistance)
            {
                Vector2 toCenter = (centerRigidbody.position - point.position).normalized;
                float excess = currentDistance - maxAllowedDistance;
                point.AddForce(toCenter * excess * stretchRecoveryForce);
            }
        }

        // ������������ ���������� �������� ����
        foreach (Rigidbody2D point in outerLayer)
        {
            float currentDistance = Vector2.Distance(centerRigidbody.position, point.position);
            float maxAllowedDistance = layerRadii[2] * maxOuterStretch;

            if (currentDistance > maxAllowedDistance)
            {
                Vector2 toCenter = (centerRigidbody.position - point.position).normalized;
                float excess = currentDistance - maxAllowedDistance;
                point.AddForce(toCenter * excess * stretchRecoveryForce);
            }
        }
    }

    void HandleOptimizedInterLayerCollisions()
    {
        // ������� �������� ����� ��������� ������
        HandleFastLayerCollisions(innerLayer, middleLayer, 0.3f, 1.2f);
        HandleFastLayerCollisions(middleLayer, outerLayer, 0.2f, 0.8f);
    }

    void HandleFastLayerCollisions(List<Rigidbody2D> innerPoints, List<Rigidbody2D> outerPoints,
                                  float innerInfluence, float outerInfluence)
    {
        // ���������� ������������� ���� ����� ������ ������ ���������
        for (int i = 0; i < Mathf.Min(innerPoints.Count, outerPoints.Count); i++)
        {
            if (innerPoints[i] == null || outerPoints[i] == null) continue;

            float distance = Vector2.Distance(innerPoints[i].position, outerPoints[i].position);
            float collisionThreshold = adaptiveCollisionRadius;

            if (distance < collisionThreshold)
            {
                Vector2 repelDir = (outerPoints[i].position - innerPoints[i].position).normalized;
                float force = Mathf.Pow(1f - (distance / collisionThreshold), layerResponseCurve) * layerRepulsionForce;

                // ���������: ���������� ����� �������� ������ �����������
                outerPoints[i].AddForce(repelDir * force * outerInfluence);
                innerPoints[i].AddForce(-repelDir * force * innerInfluence);
            }
        }
    }

    void MonitorPerformance()
    {
        physicsUpdatesPerSecond++;

        if (Time.time - lastPerformanceCheck >= 1f)
        {
            Debug.Log($"Physics UPS: {physicsUpdatesPerSecond}");
            physicsUpdatesPerSecond = 0;
            lastPerformanceCheck = Time.time;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    // ������ ��� ��������� ����������
    public void SetInnerStiffness(float stiffness) { innerLayerStiffness = stiffness; }
    public void SetMiddleStiffness(float stiffness) { middleLayerStiffness = stiffness; }
    public void SetOuterStiffness(float stiffness) { outerLayerStiffness = stiffness; }

    // ������� ������������
    public void ApplyAbilityModifiers(float massMultiplier, float sizeMultiplier, Color newColor, float duration)
    {
        StartCoroutine(AbilityModifierCoroutine(massMultiplier, sizeMultiplier, newColor, duration));
    }

    private IEnumerator AbilityModifierCoroutine(float massMultiplier, float sizeMultiplier, Color newColor, float duration)
    {
        // ��������� ������������ ��������
        float originalMassMultiplier = currentMassMultiplier;
        Vector3 originalScale = transform.localScale;
        Color originalColor = currentColor;

        // ��������� ������������
        currentMassMultiplier = massMultiplier;
        transform.localScale = originalScale * sizeMultiplier;
        currentColor = newColor;

        UpdatePhysicsWithModifiers();

        // ���� duration
        yield return new WaitForSeconds(duration);

        // ���������� ������������ ��������
        currentMassMultiplier = originalMassMultiplier;
        transform.localScale = originalScale;
        currentColor = originalColor;

        UpdatePhysicsWithModifiers();
    }

    void UpdatePhysicsWithModifiers()
    {
        // ��������� ������ � ������ �������������
        centerRigidbody.mass = 1f * currentMassMultiplier;

        foreach (Rigidbody2D rb in innerLayer)
            if (rb != null) rb.mass = 0.1f * currentMassMultiplier;

        foreach (Rigidbody2D rb in middleLayer)
            if (rb != null) rb.mass = 0.2f * currentMassMultiplier;

        foreach (Rigidbody2D rb in outerLayer)
            if (rb != null) rb.mass = 0.3f * currentMassMultiplier;
    }
}