using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SlimeCharacterController : MonoBehaviour
{
    [Header("=== КОНФИГУРАЦИЯ СЛОЕВ ===")]
    public int coreNodesCount = 6;
    public int middleNodesCount = 10;
    public int surfaceNodesCount = 14;
    public float[] layerDistances = new float[] { 0.5f, 0.9f, 1.3f };

    [Header("=== СВОЙСТВА СЛОЕВ ===")]
    public float coreStiffness = 1200f;
    public float middleStiffness = 400f;
    public float surfaceStiffness = 60f;
    public float coreDrag = 3f;
    public float surfaceDrag = 0.5f;

    [Header("=== НАСТРОЙКИ ДВИЖЕНИЯ ===")]
    public float moveSpeed = 8f;
    public float jumpInitialSpeed = 35f;
    public float jumpDecayRate = 0.85f;
    public float gravityMultiplier = 4.5f;
    public float fastFallMultiplier = 2f;
    public float obstacleDetectionRange = 0.7f;
    public float corePriorityForce = 15f;
    public float groundFriction = 10f;
    [Range(0f, 1f)]
    public float jumpGroundPercentage = 0.2f;

    [Header("=== СИСТЕМА ПРИСЕДАНИЯ ===")]
    public float crouchForce = 25f;
    public float crouchHeightReduction = 0.5f;
    public float crouchSpeedMultiplier = 0.7f;
    public float crouchGroundPullForce = 15f;

    [Header("=== ЖИДКОЕ ПОВЕДЕНИЕ ===")]
    public float surfaceOscillationForce = 2f;
    public float surfaceFluidity = 0.3f;
    public float maxSurfaceWobble = 0.4f;

    [Header("=== ПРЕДОТВРАЩЕНИЕ СТОЛКНОВЕНИЙ ===")]
    public float nodeRepelForce = 50f;
    public float unstuckForce = 100f;

    [Header("=== РАДИУСЫ АНТИ-ЗАСТРЕВАНИЯ ===")]
    public float unstuckRadius = 2.5f;
    public float autoGatherRadius = 2.0f;
    public float inputStuckRadius = 1.5f;
    [Range(0f, 1f)]
    public float autoGatherThreshold = 0.3f;
    public int maxSimultaneousGathers = 3;

    public LayerMask obstacleLayers = 1;

    [Header("=== НОВАЯ СИСТЕМА ЛАЗАНИЯ (КОЛЛАЙДЕР) ===")]
    public float wallClimbSpeed = 6f;
    public float wallStickForce = 25f;
    public float wallJumpForce = 30f;
    public float wallDetectionDistance = 0.8f;
    public float maxWallAngle = 80f;
    public float wallSlideGravity = 2f;
    public float wallClimbGravity = 1f;
    public float wallAttractionForce = 15f;

    [Header("=== СИСТЕМА ФИКСАЦИИ УЗЛОВ ===")]
    public float coreNodeReturnForce = 100f;
    public float coreNodeMaxDistance = 1.5f;
    public float middleNodeReturnForce = 70f;
    public float middleNodeMaxDistance = 2f;

    [Header("=== ОГРАНИЧЕНИЯ УЗЛОВ ===")]
    public float maxSurfaceNodeDistance = 2f;
    public float maxNodeVelocity = 15f;

    [Header("=== ДИНАМИЧЕСКИЕ СВОЙСТВА ===")]
    public float currentMass = 1f;
    public float currentSize = 1f;
    public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);

    // Node collections
    public List<Rigidbody2D> coreNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> middleNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> surfaceNodes { get; private set; } = new List<Rigidbody2D>();

    private Rigidbody2D centerBody;
    private CircleCollider2D centerCollider;
    private int surfaceGroundContactCount = 0;
    private Vector2 moveInput;
    private List<Vector2> nodeRestPositions = new List<Vector2>();
    private List<float> surfaceOscillationOffsets = new List<float>();
    private float oscillationTimer = 0f;
    private bool jumpRequested = false;
    private bool isJumping = false;
    private bool isHoldingS = false;
    private bool isCrouching = false;
    private float originalGravityScale;
    private float originalColliderRadius;

    // Jump variables
    private float currentJumpSpeed;
    private float jumpTimer;
    private const float maxJumpTime = 0.5f;

    // Система определения застревания по вводу
    private float inputStuckTimer = 0f;
    private const float INPUT_STUCK_TIME = 1.0f;
    private Vector2 lastCenterPosition;

    // Система кучкования
    private float lastGatherCheck;

    // НОВАЯ СИСТЕМА ЛАЗАНИЯ - КОЛЛАЙДЕР
    private bool isOnWall = false;
    private bool isWallClimbing = false;
    private Vector2 wallNormal = Vector2.zero;
    private Vector2 wallClimbDirection = Vector2.zero;
    private float wallStickTimer = 0f;
    private const float WALL_STICK_MAX_TIME = 3.0f;
    private ContactPoint2D[] wallContacts = new ContactPoint2D[10];
    private int wallContactCount = 0;

    void Start()
    {
        CreateCharacter();
        SetupOscillation();
        originalGravityScale = gravityMultiplier;
        lastCenterPosition = transform.position;
    }

    void CreateCharacter()
    {
        CreateCenterBody();
        CreateNodeLayers();
        ConnectNodeLayers();
        CalculateRestPositions();
    }

    void CreateCenterBody()
    {
        centerBody = GetComponent<Rigidbody2D>();
        if (centerBody == null)
            centerBody = gameObject.AddComponent<Rigidbody2D>();

        centerBody.gravityScale = 0;
        centerBody.linearDamping = 1.5f;
        centerBody.angularDamping = 2f;
        centerBody.mass = 2f * currentMass;
        centerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        centerBody.freezeRotation = true;

        centerCollider = gameObject.AddComponent<CircleCollider2D>();
        centerCollider.radius = 0.3f;
        centerCollider.isTrigger = false;
        originalColliderRadius = centerCollider.radius;
    }

    void CreateNodeLayers()
    {
        CreateNodeLayer(0, "Core", coreNodes, coreNodesCount, coreDrag, 0.05f);
        CreateNodeLayer(1, "Middle", middleNodes, middleNodesCount, 1.2f, 0.07f);
        CreateNodeLayer(2, "Surface", surfaceNodes, surfaceNodesCount, surfaceDrag, 0.09f);
    }

    void CreateNodeLayer(int layerIndex, string prefix, List<Rigidbody2D> nodes, int count, float drag, float colliderSize)
    {
        float radius = layerDistances[layerIndex];
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;

            Vector2 localPosition = direction * radius;
            Vector2 position = (Vector2)transform.position + localPosition;

            GameObject node = new GameObject($"{prefix}_Node_{i}");
            node.transform.position = position;
            node.transform.SetParent(transform);
            node.tag = "SlimeNode";

            Rigidbody2D rb = node.AddComponent<Rigidbody2D>();

            rb.gravityScale = gravityMultiplier;
            rb.linearDamping = drag;
            rb.angularDamping = drag * 1.8f;
            rb.mass = (0.3f + layerIndex * 0.15f) * currentMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            CircleCollider2D collider = node.AddComponent<CircleCollider2D>();
            collider.radius = colliderSize;

            SlimeNodeBehavior nodeBehavior = node.AddComponent<SlimeNodeBehavior>();
            nodeBehavior.Initialize(this, centerBody, layerIndex, maxSurfaceNodeDistance, maxNodeVelocity,
                                  unstuckRadius, autoGatherRadius, inputStuckRadius);

            nodes.Add(rb);
        }
    }

    void SetupOscillation()
    {
        surfaceOscillationOffsets.Clear();
        for (int i = 0; i < surfaceNodesCount; i++)
        {
            surfaceOscillationOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }

    void CalculateRestPositions()
    {
        nodeRestPositions.Clear();

        for (int i = 0; i < coreNodesCount; i++)
        {
            float angle = i * (360f / coreNodesCount);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            nodeRestPositions.Add(dir * layerDistances[0]);
        }

        for (int i = 0; i < middleNodesCount; i++)
        {
            float angle = i * (360f / middleNodesCount);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            nodeRestPositions.Add(dir * layerDistances[1]);
        }

        for (int i = 0; i < surfaceNodesCount; i++)
        {
            float angle = i * (360f / surfaceNodesCount);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            nodeRestPositions.Add(dir * layerDistances[2]);
        }
    }

    void ConnectNodeLayers()
    {
        ConnectLayerToCenter(coreNodes, 0, coreStiffness);
        ConnectLayerToCenter(middleNodes, 1, middleStiffness);
        ConnectLayerToCenter(surfaceNodes, 2, surfaceStiffness);

        ConnectNodesInLayer(coreNodes, coreStiffness * 2f, 0.9f);
        ConnectNodesInLayer(middleNodes, middleStiffness * 1.2f, 0.7f);
        ConnectNodesInLayer(surfaceNodes, surfaceStiffness * 0.8f, 0.4f);

        ConnectAdjacentLayers(coreNodes, middleNodes, 0.8f, coreStiffness);
        ConnectAdjacentLayers(middleNodes, surfaceNodes, 0.6f, middleStiffness);
    }

    void ConnectLayerToCenter(List<Rigidbody2D> nodes, int layerIndex, float stiffness)
    {
        float distance = layerDistances[layerIndex];
        foreach (Rigidbody2D node in nodes)
        {
            SpringJoint2D joint = node.gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = centerBody;
            joint.distance = distance;
            joint.dampingRatio = 0.85f;
            joint.frequency = stiffness / 150f;
            joint.autoConfigureDistance = false;
            joint.breakForce = Mathf.Infinity;
        }
    }

    void ConnectNodesInLayer(List<Rigidbody2D> nodes, float stiffness, float damping)
    {
        int count = nodes.Count;
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            SpringJoint2D joint = nodes[i].gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = nodes[next];

            float radius = GetLayerRadius(nodes);
            float circumference = 2f * Mathf.PI * radius;
            float segmentLength = circumference / count;

            joint.distance = segmentLength;
            joint.dampingRatio = damping;
            joint.frequency = stiffness / 150f;
            joint.autoConfigureDistance = false;
            joint.breakForce = Mathf.Infinity;
        }
    }

    void ConnectAdjacentLayers(List<Rigidbody2D> innerNodes, List<Rigidbody2D> outerNodes, float distanceMultiplier, float stiffness)
    {
        for (int i = 0; i < outerNodes.Count; i++)
        {
            int innerIndex = Mathf.RoundToInt((float)i / outerNodes.Count * innerNodes.Count) % innerNodes.Count;
            float distance = (GetLayerRadius(outerNodes) - GetLayerRadius(innerNodes)) * distanceMultiplier;

            SpringJoint2D joint = outerNodes[i].gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = innerNodes[innerIndex];
            joint.distance = distance;
            joint.dampingRatio = 0.8f;
            joint.frequency = stiffness / 180f;
            joint.autoConfigureDistance = false;
            joint.breakForce = Mathf.Infinity;
        }
    }

    float GetLayerRadius(List<Rigidbody2D> nodes)
    {
        if (nodes == coreNodes) return layerDistances[0];
        if (nodes == middleNodes) return layerDistances[1];
        if (nodes == surfaceNodes) return layerDistances[2];
        return 1f;
    }

    void Update()
    {
        GetPlayerInput();
        oscillationTimer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanJump())
            {
                StartJump();
            }
            else if (isOnWall)
            {
                // Прыжок от стены
                WallJump();
            }
        }

        // Новая логика для S: работает только когда на земле
        if (Input.GetKey(KeyCode.S) && IsOnGround())
        {
            isHoldingS = true;

            if (!isJumping && !isCrouching)
            {
                StartCrouch();
            }
        }
        else
        {
            isHoldingS = false;
            if (isCrouching)
            {
                StopCrouch();
            }
        }

        // Проверка застревания по вводу
        CheckInputStuck();

        // Определение лазания по стенам
        UpdateWallClimbingState();
    }

    void UpdateWallClimbingState()
    {
        // Проверяем контакт с поверхностями через коллайдер центра
        wallContactCount = centerBody.GetContacts(wallContacts);

        bool wasOnWall = isOnWall;
        isOnWall = false;
        wallNormal = Vector2.zero;

        if (wallContactCount > 0 && !IsOnGround())
        {
            // Анализируем контакты для определения стены
            Vector2 averageNormal = Vector2.zero;
            int validWallContacts = 0;

            for (int i = 0; i < wallContactCount; i++)
            {
                ContactPoint2D contact = wallContacts[i];
                float angle = Vector2.Angle(contact.normal, Vector2.up);

                // Если поверхность достаточно вертикальная для лазания
                if (angle > 30f && angle < maxWallAngle && contact.collider.CompareTag("Ground"))
                {
                    averageNormal += contact.normal;
                    validWallContacts++;
                    isOnWall = true;
                }
            }

            if (validWallContacts > 0)
            {
                wallNormal = (averageNormal / validWallContacts).normalized;

                // Определяем направление лазания на основе ввода
                if (moveInput.magnitude > 0.1f)
                {
                    Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
                    float inputAlignment = Vector2.Dot(moveInput.normalized, tangent);

                    if (Mathf.Abs(inputAlignment) > 0.3f)
                    {
                        isWallClimbing = true;
                        wallClimbDirection = tangent * Mathf.Sign(inputAlignment);
                        wallStickTimer = 0f;
                    }
                }
            }
        }

        // Если нет контактов со стеной или мы на земле - выключаем лазание
        if (!isOnWall || IsOnGround())
        {
            isWallClimbing = false;
        }

        // Автоматическое отлипание после времени
        if (isWallClimbing)
        {
            wallStickTimer += Time.deltaTime;
            if (wallStickTimer > WALL_STICK_MAX_TIME)
            {
                isWallClimbing = false;
                isOnWall = false;
            }
        }

        // Визуализация
        if (isOnWall)
        {
            Debug.DrawRay(transform.position, wallNormal * 1f, Color.red);
            if (isWallClimbing)
            {
                Debug.DrawRay(transform.position, wallClimbDirection * 1f, Color.green);
            }
        }
    }

    void CheckInputStuck()
    {
        // Проверяем, есть ли ввод, но слизь не движется
        bool hasInput = moveInput.magnitude > 0.1f;
        bool isMoving = Vector2.Distance(transform.position, lastCenterPosition) > 0.05f;

        if (hasInput && !isMoving && IsOnGround())
        {
            inputStuckTimer += Time.deltaTime;
            if (inputStuckTimer >= INPUT_STUCK_TIME)
            {
                // Активируем принудительное кучкование
                ForceGatherNodes();
                inputStuckTimer = 0f;
            }
        }
        else
        {
            inputStuckTimer = 0f;
        }

        lastCenterPosition = transform.position;
    }

    void ForceGatherNodes()
    {
        // Собираем только несколько узлов, а не все
        int nodesToGather = Mathf.Min(maxSimultaneousGathers, surfaceNodes.Count / 3);
        int gatheredCount = 0;

        foreach (var node in surfaceNodes)
        {
            if (node != null && gatheredCount < nodesToGather)
            {
                float distance = Vector2.Distance(node.position, centerBody.position);
                if (distance > inputStuckRadius)
                {
                    var nodeBehavior = node.GetComponent<SlimeNodeBehavior>();
                    if (nodeBehavior != null && !nodeBehavior.IsGathering())
                    {
                        nodeBehavior.ForceGather();
                        gatheredCount++;
                    }
                }
            }
        }
        Debug.Log($"Активировано принудительное кучкование {gatheredCount} узлов");
    }

    void FixedUpdate()
    {
        ApplyMovement();
        HandleJump();
        HandleWallClimbing();
        ApplyCrouchForces();
        ApplyLiquidBehavior();
        StabilizeNodesPosition();
        PreventNodeSticking();
        ApplyCorePriority();
        ApplyGroundFriction();
        LimitNodeVelocities();
        ApplyEnhancedGravity();
        HandleAutoGathering();
    }

    void HandleWallClimbing()
    {
        if (isWallClimbing)
        {
            // Применяем силу прилипания к стене
            centerBody.AddForce(-wallNormal * wallStickForce);

            // Двигаемся вдоль стены
            Vector2 climbVelocity = wallClimbDirection * wallClimbSpeed;
            centerBody.AddForce(climbVelocity * 10f);

            // Притягиваем узлы к стене для лучшего сцепления
            foreach (Rigidbody2D node in coreNodes)
            {
                if (node != null)
                {
                    // Вычисляем проекцию узла на стену
                    Vector2 toNode = node.position - centerBody.position;
                    float distanceToWall = Vector2.Dot(toNode, -wallNormal);

                    if (distanceToWall > 0.5f)
                    {
                        node.AddForce(-wallNormal * wallAttractionForce * 0.8f);
                    }
                }
            }

            foreach (Rigidbody2D node in middleNodes)
            {
                if (node != null)
                {
                    Vector2 toNode = node.position - centerBody.position;
                    float distanceToWall = Vector2.Dot(toNode, -wallNormal);

                    if (distanceToWall > 0.7f)
                    {
                        node.AddForce(-wallNormal * wallAttractionForce * 0.6f);
                    }
                }
            }

            // Уменьшаем гравитацию при лазании
            centerBody.AddForce(Vector2.down * wallClimbGravity);
        }
        else if (isOnWall && !IsOnGround())
        {
            // Режим соскальзывания - слабее прилипание
            centerBody.AddForce(-wallNormal * wallStickForce * 0.3f);
            centerBody.AddForce(Vector2.down * wallSlideGravity);
        }
    }

    void WallJump()
    {
        if (!isOnWall) return;

        isJumping = true;
        jumpTimer = 0f;
        isWallClimbing = false;
        isOnWall = false;

        // Прыжок от стены - отталкиваемся в противоположном направлении + вверх
        Vector2 jumpDir = (Vector2.up + (wallNormal * 0.7f)).normalized;
        currentJumpSpeed = jumpInitialSpeed * 1.3f;

        centerBody.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);

        // Также отталкиваем узлы
        foreach (Rigidbody2D node in coreNodes)
        {
            if (node != null)
                node.AddForce(jumpDir * wallJumpForce * 0.7f);
        }
        foreach (Rigidbody2D node in middleNodes)
        {
            if (node != null)
                node.AddForce(jumpDir * wallJumpForce * 0.5f);
        }
        foreach (Rigidbody2D node in surfaceNodes)
        {
            if (node != null)
                node.AddForce(jumpDir * wallJumpForce * 0.3f);
        }

        Debug.Log("Прыжок от стены!");
    }

    void StabilizeNodesPosition()
    {
        // ФИКСАЦИЯ CORE NODES - всегда возвращаются на свои позиции
        for (int i = 0; i < coreNodes.Count; i++)
        {
            if (coreNodes[i] == null) continue;

            Vector2 targetPos = (Vector2)transform.position + nodeRestPositions[i];
            Vector2 toTarget = targetPos - (Vector2)coreNodes[i].position;
            float distance = toTarget.magnitude;

            // Если узел слишком далеко, применяем сильную возвращающую силу
            if (distance > coreNodeMaxDistance)
            {
                float forceMultiplier = Mathf.Pow(distance / coreNodeMaxDistance, 2f);
                coreNodes[i].AddForce(toTarget.normalized * coreNodeReturnForce * forceMultiplier);
            }
            else if (distance > 0.3f)
            {
                // Стандартная стабилизация
                coreNodes[i].AddForce(toTarget.normalized * distance * 120f);
            }
        }

        // ФИКСАЦИЯ MIDDLE NODES - умеренное возвращение
        int middleStartIndex = coreNodesCount;
        for (int i = 0; i < middleNodes.Count; i++)
        {
            if (middleNodes[i] == null) continue;

            Vector2 targetPos = (Vector2)transform.position + nodeRestPositions[middleStartIndex + i];
            Vector2 toTarget = targetPos - (Vector2)middleNodes[i].position;
            float distance = toTarget.magnitude;

            if (distance > middleNodeMaxDistance)
            {
                float forceMultiplier = Mathf.Pow(distance / middleNodeMaxDistance, 1.5f);
                middleNodes[i].AddForce(toTarget.normalized * middleNodeReturnForce * forceMultiplier);
            }
            else if (distance > 0.4f)
            {
                middleNodes[i].AddForce(toTarget.normalized * distance * 80f);
            }
        }
    }

    void GetPlayerInput()
    {
        moveInput = Vector2.zero;

        // Только горизонтальное движение
        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
        moveInput = moveInput.normalized;
    }

    void ApplyMovement()
    {
        if (moveInput.magnitude > 0.1f && !isWallClimbing)
        {
            float currentMoveSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;

            Vector2 targetVelocity = moveInput * currentMoveSpeed;
            Vector2 velocityChange = targetVelocity - centerBody.linearVelocity;

            centerBody.AddForce(velocityChange * 20f);
        }
    }

    void ApplyGroundFriction()
    {
        if (IsOnGround() && moveInput.magnitude < 0.1f && centerBody.linearVelocity.magnitude < 0.5f)
        {
            centerBody.linearVelocity = new Vector2(0f, centerBody.linearVelocity.y);
        }
    }

    void ApplyEnhancedGravity()
    {
        if (isWallClimbing || isOnWall) return; // Не применяем усиленную гравитацию при лазании

        // Усиленная гравитация для более быстрого падения
        foreach (Rigidbody2D node in coreNodes)
            if (node != null) node.AddForce(Vector2.down * 5f);
        foreach (Rigidbody2D node in middleNodes)
            if (node != null) node.AddForce(Vector2.down * 8f);
        foreach (Rigidbody2D node in surfaceNodes)
            if (node != null) node.AddForce(Vector2.down * 10f);
    }

    void StartCrouch()
    {
        isCrouching = true;
        // Уменьшаем коллайдер центра при приседании
        if (centerCollider != null)
        {
            centerCollider.radius = originalColliderRadius * crouchHeightReduction;
        }
    }

    void StopCrouch()
    {
        isCrouching = false;
        // Восстанавливаем коллайдер центра
        if (centerCollider != null)
        {
            centerCollider.radius = originalColliderRadius;
        }
    }

    void ApplyCrouchForces()
    {
        if (isCrouching && IsOnGround())
        {
            // Прижимаем центр к земле
            centerBody.AddForce(Vector2.down * crouchGroundPullForce);

            foreach (var node in surfaceNodes)
            {
                if (node == null) continue;
                if (node.position.y > centerBody.position.y)
                {
                    node.AddForce(Vector2.down * crouchForce);
                }
            }

            foreach (var node in middleNodes)
            {
                if (node == null) continue;
                if (node.position.y > centerBody.position.y)
                {
                    node.AddForce(Vector2.down * crouchForce * 0.7f);
                }
            }
        }
    }

    void HandleAutoGathering()
    {
        // Проверяем не чаще чем раз в 0.5 секунды
        if (Time.time - lastGatherCheck < 0.5f) return;
        lastGatherCheck = Time.time;

        // Проверяем, нужно ли автоматическое кучкование
        int nodesOutsideRadius = 0;
        foreach (var node in surfaceNodes)
        {
            if (node != null)
            {
                float distance = Vector2.Distance(node.position, centerBody.position);
                if (distance > autoGatherRadius)
                {
                    nodesOutsideRadius++;
                }
            }
        }

        float percentageOutside = (float)nodesOutsideRadius / surfaceNodes.Count;

        if (percentageOutside > autoGatherThreshold)
        {
            // Вычисляем сколько узлов нужно вернуть для поддержания порога
            int targetNodesOutside = Mathf.FloorToInt(surfaceNodes.Count * autoGatherThreshold);
            int nodesToGather = Mathf.Min(nodesOutsideRadius - targetNodesOutside, maxSimultaneousGathers);

            int gatheredCount = 0;

            // Активируем кучкование для нескольких внешних узлов за радиусом
            foreach (var node in surfaceNodes)
            {
                if (node != null && gatheredCount < nodesToGather)
                {
                    float distance = Vector2.Distance(node.position, centerBody.position);
                    if (distance > autoGatherRadius)
                    {
                        var nodeBehavior = node.GetComponent<SlimeNodeBehavior>();
                        if (nodeBehavior != null && !nodeBehavior.IsGathering())
                        {
                            nodeBehavior.StartAutoGather();
                            gatheredCount++;
                        }
                    }
                }
            }

            if (gatheredCount > 0)
            {
                Debug.Log($"Автоматическое кучкование: {gatheredCount} узлов (вне радиуса: {nodesOutsideRadius})");
            }
        }
    }

    void StartJump()
    {
        if (!isJumping && CanJump())
        {
            isJumping = true;
            jumpTimer = 0f;

            currentJumpSpeed = jumpInitialSpeed / Mathf.Max(0.5f, currentMass);

            // Усиление прыжка при приседании
            if (isHoldingS)
            {
                currentJumpSpeed *= 1.5f;
            }
        }
    }

    void HandleJump()
    {
        if (isJumping)
        {
            jumpTimer += Time.fixedDeltaTime;

            if (jumpTimer < maxJumpTime)
            {
                float jumpForce = currentJumpSpeed * Mathf.Pow(jumpDecayRate, jumpTimer * 10f);

                centerBody.AddForce(Vector2.up * jumpForce);

                foreach (Rigidbody2D node in coreNodes)
                {
                    if (node != null)
                        node.AddForce(Vector2.up * jumpForce * 0.7f);
                }
                foreach (Rigidbody2D node in middleNodes)
                {
                    if (node != null)
                        node.AddForce(Vector2.up * jumpForce * 0.5f);
                }
                foreach (Rigidbody2D node in surfaceNodes)
                {
                    if (node != null)
                        node.AddForce(Vector2.up * jumpForce * 0.3f);
                }
            }
            else
            {
                EndJump();
            }
        }
    }

    void EndJump()
    {
        isJumping = false;
    }

    void LimitNodeVelocities()
    {
        foreach (Rigidbody2D node in coreNodes)
        {
            if (node != null && node.linearVelocity.magnitude > maxNodeVelocity)
            {
                node.linearVelocity = node.linearVelocity.normalized * maxNodeVelocity;
            }
        }
        foreach (Rigidbody2D node in middleNodes)
        {
            if (node != null && node.linearVelocity.magnitude > maxNodeVelocity)
            {
                node.linearVelocity = node.linearVelocity.normalized * maxNodeVelocity;
            }
        }
        foreach (Rigidbody2D node in surfaceNodes)
        {
            if (node != null && node.linearVelocity.magnitude > maxNodeVelocity)
            {
                node.linearVelocity = node.linearVelocity.normalized * maxNodeVelocity;
            }
        }
    }

    void ApplyCorePriority()
    {
        if (moveInput.magnitude > 0.1f && !isWallClimbing)
        {
            Vector2 moveDirection = moveInput.normalized;

            foreach (Rigidbody2D node in coreNodes)
            {
                if (node == null) continue;

                Vector2 toCenter = (centerBody.position - node.position);
                float alignment = Vector2.Dot(toCenter.normalized, moveDirection);

                if (alignment < 0.5f)
                {
                    node.AddForce(moveDirection * corePriorityForce * 0.8f);
                }
            }

            foreach (Rigidbody2D node in middleNodes)
            {
                if (node == null) continue;

                Vector2 toCenter = (centerBody.position - node.position);
                float alignment = Vector2.Dot(toCenter.normalized, moveDirection);

                if (alignment < 0.3f)
                {
                    node.AddForce(moveDirection * corePriorityForce * 0.5f);
                }
            }

            foreach (Rigidbody2D node in surfaceNodes)
            {
                if (node == null) continue;

                Vector2 toCenter = (centerBody.position - node.position);
                float alignment = Vector2.Dot(toCenter.normalized, moveDirection);

                if (alignment < 0.1f)
                {
                    node.AddForce(moveDirection * corePriorityForce * 0.3f);
                }
            }
        }
    }

    void ApplyLiquidBehavior()
    {
        for (int i = 0; i < surfaceNodes.Count; i++)
        {
            if (surfaceNodes[i] == null) continue;

            float oscillation = Mathf.Sin(oscillationTimer * 2f + surfaceOscillationOffsets[i]) * surfaceOscillationForce;
            Vector2 oscillationDir = GetTangentialDirection(surfaceNodes[i].position, centerBody.position);

            surfaceNodes[i].AddForce(oscillationDir * oscillation * surfaceFluidity);
        }
    }

    Vector2 GetTangentialDirection(Vector2 nodePos, Vector2 centerPos)
    {
        Vector2 toCenter = centerPos - nodePos;
        return new Vector2(-toCenter.y, toCenter.x).normalized;
    }

    void PreventNodeSticking()
    {
        foreach (Rigidbody2D node in surfaceNodes)
        {
            if (node == null) continue;

            Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(node.position, 0.2f, obstacleLayers);
            foreach (Collider2D obstacle in nearbyObstacles)
            {
                if (obstacle.CompareTag("Ground"))
                {
                    Vector2 repelDir = (node.position - (Vector2)obstacle.transform.position).normalized;
                    if (repelDir.magnitude < 0.1f) repelDir = Random.insideUnitCircle.normalized;

                    node.AddForce(repelDir * nodeRepelForce);
                }
            }
        }
    }

    public void ReportSurfaceGroundContact(bool isContact)
    {
        if (isContact)
            surfaceGroundContactCount++;
        else
            surfaceGroundContactCount--;

        surfaceGroundContactCount = Mathf.Clamp(surfaceGroundContactCount, 0, surfaceNodesCount);
    }

    bool IsOnGround()
    {
        return surfaceGroundContactCount > 0;
    }

    bool CanJump()
    {
        float groundPercentage = (float)surfaceGroundContactCount / surfaceNodesCount;
        return groundPercentage >= jumpGroundPercentage;
    }

    public List<Rigidbody2D> GetNodesByLayer(int layerIndex)
    {
        switch (layerIndex)
        {
            case 0: return coreNodes;
            case 1: return middleNodes;
            case 2: return surfaceNodes;
            default: return new List<Rigidbody2D>();
        }
    }

    // Методы для визуализации в редакторе
    void OnDrawGizmosSelected()
    {
        // Визуализация всех радиусов
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, unstuckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoGatherRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, inputStuckRadius);

        // Визуализация состояния лазания
        if (isOnWall)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            if (isWallClimbing)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }

    public float GetGroundContactPercentage()
    {
        return (float)surfaceGroundContactCount / surfaceNodesCount * 100f;
    }

    public Vector2 GetCenterVelocity()
    {
        return centerBody != null ? centerBody.linearVelocity : Vector2.zero;
    }

    public bool IsJumping()
    {
        return isJumping;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool IsOnWall()
    {
        return isOnWall;
    }

    public bool IsWallClimbing()
    {
        return isWallClimbing;
    }
}