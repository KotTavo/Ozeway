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
    public float climbAssistForce = 12f;
    public float obstacleDetectionRange = 0.7f;
    public float corePriorityForce = 15f;
    public float groundFriction = 10f;
    [Range(0f, 1f)]
    public float jumpGroundPercentage = 0.2f;

    [Header("=== ЖИДКОЕ ПОВЕДЕНИЕ ===")]
    public float surfaceOscillationForce = 2f;
    public float surfaceFluidity = 0.3f;
    public float maxSurfaceWobble = 0.4f;

    [Header("=== ПРЕДОТВРАЩЕНИЕ СТОЛКНОВЕНИЙ ===")]
    public float nodeRepelForce = 50f;
    public float unstuckForce = 100f;
    public float maxUnstuckDistance = 2f;
    public LayerMask obstacleLayers = 1;

    [Header("=== НАСТРОЙКИ ИНЕРЦИИ ===")]
    public float middleLayerDelay = 0.15f;
    public float surfaceLayerDelay = 0.15f;

    [Header("=== ОГРАНИЧЕНИЯ УЗЛОВ ===")]
    public float maxSurfaceNodeDistance = 2f;
    public float maxNodeVelocity = 15f;

    [Header("=== НАСТРОЙКИ ПОДНЯТИЯ ===")]
    public float liftForce = 15f;
    public float maxLiftHeight = 3f;
    public float liftHeightPercent = 0.3f;

    [Header("=== ДИНАМИЧЕСКИЕ СВОЙСТВА ===")]
    public float currentMass = 1f;
    public float currentSize = 1f;
    public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);

    // Node collections
    public List<Rigidbody2D> coreNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> middleNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> surfaceNodes { get; private set; } = new List<Rigidbody2D>();

    private Rigidbody2D centerBody;
    private int surfaceGroundContactCount = 0;
    private Vector2 moveInput;
    private List<Vector2> nodeRestPositions = new List<Vector2>();
    private List<float> surfaceOscillationOffsets = new List<float>();
    private float oscillationTimer = 0f;
    private List<bool> nodeStuckStatus = new List<bool>();
    private float lastUnstuckCheck;
    private bool jumpRequested = false;
    private bool isJumping = false;
    private bool isHoldingS = false;
    private bool isHoldingW = false;
    private bool isLifted = false;
    private float originalGravityScale;

    // Jump variables
    private float currentJumpSpeed;
    private float jumpTimer;
    private const float maxJumpTime = 0.5f;

    // Инерция для слоев
    private List<Vector2> middleLayerTargetPositions = new List<Vector2>();
    private List<Vector2> surfaceLayerTargetPositions = new List<Vector2>();
    private float middleLayerTimer = 0f;
    private float surfaceLayerTimer = 0f;

    void Start()
    {
        CreateCharacter();
        SetupOscillation();
        InitializeInertia();
        originalGravityScale = gravityMultiplier;
    }

    void CreateCharacter()
    {
        CreateCenterBody();
        CreateNodeLayers();
        ConnectNodeLayers();
        CalculateRestPositions();
        InitializeStuckTracking();
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

        CircleCollider2D centerCollider = gameObject.AddComponent<CircleCollider2D>();
        centerCollider.radius = 0.3f;
        centerCollider.isTrigger = false;
    }

    void CreateNodeLayers()
    {
        // Core layer - solid and round
        CreateNodeLayer(0, "Core", coreNodes, coreNodesCount, coreDrag, 0.05f, 0.3f);
        // Middle layer - transitional
        CreateNodeLayer(1, "Middle", middleNodes, middleNodesCount, 1.2f, 0.07f, 0.6f);
        // Surface layer - liquid and wobbly
        CreateNodeLayer(2, "Surface", surfaceNodes, surfaceNodesCount, surfaceDrag, 0.09f, 1f);
    }

    void CreateNodeLayer(int layerIndex, string prefix, List<Rigidbody2D> nodes, int count, float drag, float colliderSize, float gravityEffect)
    {
        float radius = layerDistances[layerIndex];
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.up;

            // Используем локальную позицию относительно центра
            Vector2 localPosition = direction * radius;
            Vector2 position = (Vector2)transform.position + localPosition;

            GameObject node = new GameObject($"{prefix}_Node_{i}");
            node.transform.position = position;
            node.transform.SetParent(transform);
            node.tag = "SlimeNode";

            Rigidbody2D rb = node.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravityMultiplier * gravityEffect;
            rb.linearDamping = drag;
            rb.angularDamping = drag * 1.8f;
            rb.mass = (0.3f + layerIndex * 0.15f) * currentMass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            CircleCollider2D collider = node.AddComponent<CircleCollider2D>();
            collider.radius = colliderSize;

            SlimeNodeBehavior nodeBehavior = node.AddComponent<SlimeNodeBehavior>();
            nodeBehavior.Initialize(this, centerBody, layerIndex, maxSurfaceNodeDistance, maxNodeVelocity);

            nodes.Add(rb);
        }
    }

    void InitializeStuckTracking()
    {
        nodeStuckStatus.Clear();
        int totalNodes = coreNodesCount + middleNodesCount + surfaceNodesCount;
        for (int i = 0; i < totalNodes; i++)
        {
            nodeStuckStatus.Add(false);
        }
    }

    void InitializeInertia()
    {
        middleLayerTargetPositions.Clear();
        surfaceLayerTargetPositions.Clear();

        for (int i = 0; i < middleNodes.Count; i++)
        {
            middleLayerTargetPositions.Add(middleNodes[i].position);
        }

        for (int i = 0; i < surfaceNodes.Count; i++)
        {
            surfaceLayerTargetPositions.Add(surfaceNodes[i].position);
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
        // Connect to center
        ConnectLayerToCenter(coreNodes, 0, coreStiffness);
        ConnectLayerToCenter(middleNodes, 1, middleStiffness);
        ConnectLayerToCenter(surfaceNodes, 2, surfaceStiffness);

        // Connect within layers - core is tightly connected
        ConnectNodesInLayer(coreNodes, coreStiffness * 2f, 0.9f);
        ConnectNodesInLayer(middleNodes, middleStiffness * 1.2f, 0.7f);
        // Surface layer has looser connections for fluidity
        ConnectNodesInLayer(surfaceNodes, surfaceStiffness * 0.8f, 0.4f);

        // Connect between layers
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

        // Handle jump input in Update for better responsiveness
        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            StartJump();
        }

        // Handle W key for lifting - новая логика
        if (Input.GetKey(KeyCode.W) && IsOnGround() && !isJumping)
        {
            isHoldingW = true;
            if (!isLifted)
            {
                StartLift();
            }
        }
        else
        {
            isHoldingW = false;
            if (isLifted)
            {
                StopLift();
            }
        }

        // Handle S key for enhanced jump and fast fall
        if (Input.GetKey(KeyCode.S))
        {
            isHoldingS = true;
            // Fast fall when not jumping and in air
            if (!isJumping && !IsOnGround())
            {
                ApplyFastFall();
            }
        }
        else
        {
            isHoldingS = false;
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        HandleJump();
        ApplyLift();
        ApplyLiquidBehavior();
        StabilizeCoreLayer();
        HandleObstacleClimbing();
        PreventNodeSticking();
        HandleStuckNodes();
        ApplyCorePriority();
        ApplyGroundFriction();
        ApplyLayerInertia();
        LimitNodeVelocities();
    }

    void GetPlayerInput()
    {
        moveInput = Vector2.zero;

        // During jump, W and S don't work for movement
        if (!isJumping)
        {
            if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
        }

        if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
        if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
        moveInput = moveInput.normalized;
    }

    void ApplyMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector2 targetVelocity = moveInput * moveSpeed;
            Vector2 velocityChange = targetVelocity - centerBody.linearVelocity;

            // Apply stronger force for more responsive movement
            centerBody.AddForce(velocityChange * 20f);
        }
    }

    void ApplyGroundFriction()
    {
        // Apply friction when on ground and not moving
        if (IsOnGround() && moveInput.magnitude < 0.1f && centerBody.linearVelocity.magnitude < 0.5f)
        {
            // Stop completely when velocity is low and no input
            centerBody.linearVelocity = new Vector2(0f, centerBody.linearVelocity.y);
        }
    }

    void ApplyLayerInertia()
    {
        // Update middle layer with delay
        middleLayerTimer += Time.fixedDeltaTime;
        if (middleLayerTimer >= middleLayerDelay)
        {
            for (int i = 0; i < middleNodes.Count; i++)
            {
                if (middleNodes[i] != null)
                {
                    middleLayerTargetPositions[i] = middleNodes[i].position;
                }
            }
            middleLayerTimer = 0f;
        }

        // Update surface layer with delay
        surfaceLayerTimer += Time.fixedDeltaTime;
        if (surfaceLayerTimer >= surfaceLayerDelay)
        {
            for (int i = 0; i < surfaceNodes.Count; i++)
            {
                if (surfaceNodes[i] != null)
                {
                    surfaceLayerTargetPositions[i] = surfaceNodes[i].position;
                }
            }
            surfaceLayerTimer = 0f;
        }

        // Apply smooth movement to middle layer
        for (int i = 0; i < middleNodes.Count; i++)
        {
            if (middleNodes[i] != null)
            {
                Vector2 targetPos = middleLayerTargetPositions[i];
                Vector2 currentPos = middleNodes[i].position;
                Vector2 direction = (targetPos - currentPos).normalized;
                float distance = Vector2.Distance(targetPos, currentPos);

                if (distance > 0.1f)
                {
                    middleNodes[i].AddForce(direction * distance * 50f);
                }
            }
        }

        // Apply smooth movement to surface layer
        for (int i = 0; i < surfaceNodes.Count; i++)
        {
            if (surfaceNodes[i] != null)
            {
                Vector2 targetPos = surfaceLayerTargetPositions[i];
                Vector2 currentPos = surfaceNodes[i].position;
                Vector2 direction = (targetPos - currentPos).normalized;
                float distance = Vector2.Distance(targetPos, currentPos);

                if (distance > 0.1f)
                {
                    surfaceNodes[i].AddForce(direction * distance * 30f);
                }
            }
        }
    }

    void StartLift()
    {
        isLifted = true;
        // Новая логика: просто устанавливаем флаг, сила применяется в ApplyLift
    }

    void StopLift()
    {
        isLifted = false;
    }

    void ApplyLift()
    {
        if (isLifted && isHoldingW && IsOnGround())
        {
            // Определяем целевую высоту поднятия
            float targetHeight = GetGroundHeight() + maxLiftHeight * liftHeightPercent;

            // Если текущая высота меньше целевой, применяем силу
            if (transform.position.y < targetHeight)
            {
                // Применяем силу поднятия ко всем узлам
                foreach (Rigidbody2D node in coreNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * liftForce * 0.8f);
                }
                foreach (Rigidbody2D node in middleNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * liftForce * 0.6f);
                }
                foreach (Rigidbody2D node in surfaceNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * liftForce * 0.4f);
                }

                // Также поднимаем центр
                centerBody.AddForce(Vector2.up * liftForce * 0.5f);
            }
        }
    }

    float GetGroundHeight()
    {
        // Находим высоту земли под слизью
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, obstacleLayers);
        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            return hit.point.y;
        }
        return transform.position.y;
    }

    void ApplyFastFall()
    {
        // Увеличиваем гравитацию для быстрого падения
        foreach (Rigidbody2D node in coreNodes)
            if (node != null) node.gravityScale = gravityMultiplier * fastFallMultiplier * 0.3f;
        foreach (Rigidbody2D node in middleNodes)
            if (node != null) node.gravityScale = gravityMultiplier * fastFallMultiplier * 0.6f;
        foreach (Rigidbody2D node in surfaceNodes)
            if (node != null) node.gravityScale = gravityMultiplier * fastFallMultiplier * 1f;
    }

    void StartJump()
    {
        if (!isJumping && CanJump())
        {
            isJumping = true;
            jumpRequested = false;
            jumpTimer = 0f;

            // Начальная скорость зависит от массы (чем больше масса, тем меньше скорость)
            currentJumpSpeed = jumpInitialSpeed / Mathf.Max(0.5f, currentMass);

            // Усиливаем прыжок при зажатии S
            if (isHoldingS)
            {
                currentJumpSpeed *= 1.5f;
            }

            // Восстанавливаем нормальную гравитацию перед прыжком
            gravityMultiplier = originalGravityScale;
            UpdateNodesGravity();
        }
    }

    void HandleJump()
    {
        if (isJumping)
        {
            jumpTimer += Time.fixedDeltaTime;

            if (jumpTimer < maxJumpTime)
            {
                // Экспоненциальное уменьшение скорости прыжка
                float jumpForce = currentJumpSpeed * Mathf.Pow(jumpDecayRate, jumpTimer * 10f);

                // Применяем силу прыжка к центру и всем узлам
                centerBody.AddForce(Vector2.up * jumpForce);

                foreach (Rigidbody2D node in coreNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * jumpForce * 0.7f);
                }
                foreach (Rigidbody2D node in middleNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * jumpForce * 0.5f);
                }
                foreach (Rigidbody2D node in surfaceNodes)
                {
                    if (node != null) node.AddForce(Vector2.up * jumpForce * 0.3f);
                }
            }
            else
            {
                // Завершаем прыжок
                EndJump();
            }
        }
    }

    void EndJump()
    {
        isJumping = false;
    }

    void UpdateNodesGravity()
    {
        // Обновляем гравитацию для всех узлов
        foreach (Rigidbody2D node in coreNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 0.3f;
        foreach (Rigidbody2D node in middleNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 0.6f;
        foreach (Rigidbody2D node in surfaceNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 1f;
    }

    void LimitNodeVelocities()
    {
        // Ограничиваем максимальную скорость всех узлов
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
        // Apply force to help nodes follow the core when moving through obstacles
        if (moveInput.magnitude > 0.1f)
        {
            Vector2 moveDirection = moveInput.normalized;

            // Help core nodes follow first
            foreach (Rigidbody2D node in coreNodes)
            {
                if (node == null) continue;

                Vector2 toCenter = (centerBody.position - node.position);
                float alignment = Vector2.Dot(toCenter.normalized, moveDirection);

                if (alignment < 0.5f) // If node is not aligned with movement
                {
                    node.AddForce(moveDirection * corePriorityForce * 0.8f);
                }
            }

            // Help other nodes follow
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
        // Apply oscillation to surface nodes for liquid effect
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

    void StabilizeCoreLayer()
    {
        // Keep core layer tightly formed and round
        for (int i = 0; i < coreNodes.Count; i++)
        {
            if (coreNodes[i] == null) continue;

            Vector2 targetPos = (Vector2)transform.position + nodeRestPositions[i];
            Vector2 toTarget = targetPos - (Vector2)coreNodes[i].position;
            float distance = toTarget.magnitude;

            if (distance > 0.2f)
            {
                coreNodes[i].AddForce(toTarget.normalized * distance * 80f);
            }
        }
    }

    void HandleObstacleClimbing()
    {
        // Check for obstacles in movement direction
        if (moveInput.magnitude > 0.1f)
        {
            Vector2 checkDir = moveInput.normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, checkDir, obstacleDetectionRange, obstacleLayers);

            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                // Check if obstacle is low enough to climb
                RaycastHit2D topHit = Physics2D.Raycast(
                    (Vector2)transform.position + checkDir * obstacleDetectionRange,
                    Vector2.up, 0.5f, obstacleLayers);

                if (topHit.collider == null)
                {
                    // Apply stronger climb force to help overcome obstacles
                    centerBody.AddForce((Vector2.up * 0.7f + checkDir * 0.3f) * climbAssistForce);

                    // Help nodes climb over - but with less force than the core
                    foreach (Rigidbody2D node in surfaceNodes)
                    {
                        if (node == null) continue;
                        node.AddForce((Vector2.up * 0.5f + checkDir * 0.2f) * climbAssistForce * 0.4f);
                    }
                }
                else
                {
                    // If obstacle is too high, help nodes flow around it
                    Vector2 flowDirection = new Vector2(-checkDir.y, checkDir.x); // Perpendicular direction
                    foreach (Rigidbody2D node in surfaceNodes)
                    {
                        if (node == null) continue;

                        // Check if this node is likely stuck behind the obstacle
                        float nodeDistance = Vector2.Distance(node.position, hit.point);
                        if (nodeDistance < 0.5f)
                        {
                            node.AddForce(flowDirection * climbAssistForce * 0.3f);
                        }
                    }
                }
            }
        }
    }

    void PreventNodeSticking()
    {
        // Apply repulsive force to nodes that are too close to obstacles
        foreach (Rigidbody2D node in surfaceNodes)
        {
            if (node == null) continue;

            // Check for nearby obstacles
            Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(node.position, 0.2f, obstacleLayers);
            foreach (Collider2D obstacle in nearbyObstacles)
            {
                if (obstacle.CompareTag("Ground"))
                {
                    // Calculate repulsion direction
                    Vector2 repelDir = (node.position - (Vector2)obstacle.transform.position).normalized;
                    if (repelDir.magnitude < 0.1f) repelDir = Random.insideUnitCircle.normalized;

                    node.AddForce(repelDir * nodeRepelForce);
                }
            }
        }
    }

    void HandleStuckNodes()
    {
        // Check for stuck nodes periodically
        if (Time.time - lastUnstuckCheck < 0.5f) return;
        lastUnstuckCheck = Time.time;

        // Check all nodes for being stuck
        CheckNodeGroupStuck(coreNodes, 0);
        CheckNodeGroupStuck(middleNodes, coreNodesCount);
        CheckNodeGroupStuck(surfaceNodes, coreNodesCount + middleNodesCount);
    }

    void CheckNodeGroupStuck(List<Rigidbody2D> nodes, int startIndex)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;

            int globalIndex = startIndex + i;
            float distanceFromCenter = Vector2.Distance(centerBody.position, nodes[i].position);
            float maxAllowedDistance = GetMaxDistanceForLayer(nodes) * 1.5f;

            // Node is stuck if it's too far from center and not moving much
            bool isStuck = distanceFromCenter > maxAllowedDistance &&
                          nodes[i].linearVelocity.magnitude < 0.1f;

            if (isStuck && !nodeStuckStatus[globalIndex])
            {
                // Node just became stuck
                nodeStuckStatus[globalIndex] = true;
                UnstuckNode(nodes[i]);
            }
            else if (!isStuck && nodeStuckStatus[globalIndex])
            {
                // Node is no longer stuck
                nodeStuckStatus[globalIndex] = false;
            }
        }
    }

    float GetMaxDistanceForLayer(List<Rigidbody2D> nodes)
    {
        if (nodes == coreNodes) return layerDistances[0];
        if (nodes == middleNodes) return layerDistances[1];
        if (nodes == surfaceNodes) return layerDistances[2];
        return 1f;
    }

    void UnstuckNode(Rigidbody2D node)
    {
        // Calculate direction back to center
        Vector2 toCenter = (centerBody.position - node.position).normalized;

        // Add some randomness to prevent nodes from getting stuck in the same place
        Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
        Vector2 unstuckDirection = (toCenter + randomOffset).normalized;

        // Apply unstuck force
        node.AddForce(unstuckDirection * unstuckForce, ForceMode2D.Impulse);

        // Also move the node slightly towards center
        node.MovePosition(node.position + unstuckDirection * 0.2f);
    }

    // Public method for surface nodes to report ground contact
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

    // Public method for nodes to report being stuck
    public void ReportNodeStuck(Rigidbody2D node)
    {
        // Find the node in our lists and mark it as stuck
        int index = FindNodeIndex(node);
        if (index >= 0 && index < nodeStuckStatus.Count)
        {
            nodeStuckStatus[index] = true;
        }
    }

    int FindNodeIndex(Rigidbody2D node)
    {
        for (int i = 0; i < coreNodes.Count; i++)
        {
            if (coreNodes[i] == node) return i;
        }
        for (int i = 0; i < middleNodes.Count; i++)
        {
            if (middleNodes[i] == node) return coreNodesCount + i;
        }
        for (int i = 0; i < surfaceNodes.Count; i++)
        {
            if (surfaceNodes[i] == node) return coreNodesCount + middleNodesCount + i;
        }
        return -1;
    }

    // Public methods for ability system
    public void ModifySlimeProperties(float massChange, float sizeChange, Color newColor, float duration)
    {
        StartCoroutine(TemporaryModification(massChange, sizeChange, newColor, duration));
    }

    private IEnumerator TemporaryModification(float massChange, float sizeChange, Color newColor, float duration)
    {
        float originalMass = currentMass;
        Vector3 originalScale = transform.localScale;
        Color originalColor = slimeColor;

        currentMass = massChange;
        transform.localScale = originalScale * sizeChange;
        slimeColor = newColor;

        UpdateMassProperties();

        yield return new WaitForSeconds(duration);

        currentMass = originalMass;
        transform.localScale = originalScale;
        slimeColor = originalColor;

        UpdateMassProperties();
    }

    void UpdateMassProperties()
    {
        centerBody.mass = 2f * currentMass;

        foreach (Rigidbody2D node in coreNodes)
            if (node != null) node.mass = 0.3f * currentMass;

        foreach (Rigidbody2D node in middleNodes)
            if (node != null) node.mass = 0.45f * currentMass;

        foreach (Rigidbody2D node in surfaceNodes)
            if (node != null) node.mass = 0.6f * currentMass;
    }

    // Public methods for info display
    public float GetGroundContactPercentage()
    {
        return (float)surfaceGroundContactCount / surfaceNodesCount * 100f;
    }

    public Vector2 GetCenterVelocity()
    {
        return centerBody.linearVelocity;
    }

    public bool IsLifted()
    {
        return isLifted;
    }

    public bool IsJumping()
    {
        return isJumping;
    }
}