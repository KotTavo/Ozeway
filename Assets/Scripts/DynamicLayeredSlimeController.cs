using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class SlimeCharacterController : MonoBehaviour
{
    [Header("=== КОНФИГУРАЦИЯ СЛОЕВ ===")]
    public int coreNodesCount = 6;
    public int middleNodesCount = 10;
    public int surfaceNodesCount = 14;
    public float[] layerDistances = new float[] { 0.5f, 0.9f, 1.3f };

    [Header("=== СВОЙСТВА СЛОЕВ ===")]
    public float coreStiffness = 2000f;
    public float middleStiffness = 800f;
    public float surfaceStiffness = 100f;
    public float coreDrag = 3f;
    public float surfaceDrag = 0.5f;

    [Header("=== СВОБОДНОЕ ВРАЩЕНИЕ СРЕДНИХ УЗЛОВ ===")]
    public bool enableFreeRotation = true;
    public float rotationFreedom = 360f;
    public float rotationReturnForce = 5f;
    public float maxRotationSpeed = 180f;

    [Header("=== СИСТЕМА ПРОХОДА СКВОЗЬ ПРЕПЯТСТВИЯ ===")]
    public float teleportThreshold = 3.5f;
    public float teleportCooldown = 1f;
    public bool enableTeleportThroughObstacles = true;
    public float minTeleportDistance = 0.5f;
    public float teleportForce = 30f;

    [Header("=== НАСТРОЙКИ ДВИЖЕНИЯ ===")]
    public float moveSpeed = 8f;
    public float jumpInitialSpeed = 35f;
    public float jumpDecayRate = 0.85f;
    public float gravityMultiplier = 4.5f;
    public float fastFallMultiplier = 2f;
    public float climbAssistForce = 12f;
    public float obstacleDetectionRange = 0.7f;
    public float corePriorityForce = 15f;
    public float groundFriction = 15f;
    [Range(0f, 1f)]
    public float jumpGroundPercentage = 0.2f;

    [Header("=== ЖИДКОЕ ПОВЕДЕНИЕ ===")]
    public float surfaceOscillationForce = 2f;
    public float surfaceFluidity = 0.3f;
    public float maxSurfaceWobble = 0.4f;

    [Header("=== СИСТЕМА СКОЛЬЖЕНИЯ И КАРАБКАНИЯ ===")]
    public float surfaceSlideForce = 25f;
    public float slideDetectionRange = 0.8f;
    public float maxSlideAngle = 75f;
    public float slideAcceleration = 2f;
    public bool enableSurfaceSliding = true;
    public float autoClimbForce = 35f;
    public float climbDetectionRange = 0.6f;
    public float maxAutoClimbHeight = 2.5f;
    public float climbAssistMultiplier = 1.5f;

    [Header("=== ИЕРАРХИЧЕСКИЕ СИЛЫ ===")]
    public float coreToMiddleForce = 60f; // Увеличил для компенсации отсутствия пружин
    public float middleToSurfaceForce = 35f;
    public float hierarchicalInfluenceRadius = 2f;
    public float maxHierarchicalForce = 100f; // Увеличил максимальную силу
    public bool enableHierarchicalForces = true;

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

    [Header("=== СИСТЕМА ВЫТЯГИВАНИЯ ===")]
    public float stretchUpwardForce = 12f;
    public float stretchHorizontalForce = 8f;
    public float maxStretchHeight = 2.5f;
    public float minGroundContactPercent = 0.3f;
    public float stretchReturnSpeed = 4f;
    public float stretchMoveSpeedMultiplier = 0.67f;
    public float stretchJumpMultiplier = 0.5f;

    [Header("=== СТАБИЛИЗАЦИЯ ПОКОЯ И СПОЛЗАНИЕ ===")]
    public float staticFrictionForce = 25f;
    public float velocityStoppingThreshold = 0.3f;
    public float groundStabilizationForce = 35f;
    public float maxStableSlopeAngle = 45f;
    public float slideDownForce = 8f;
    public float slopeStabilizationThreshold = 0.5f;
    public float positionLockThreshold = 0.1f;
    public float positionLockForce = 50f;

    [Header("=== ДИНАМИЧЕСКИЕ СВОЙСТВА ===")]
    public float currentMass = 1f;
    public float currentSize = 1f;
    public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);

    [Header("=== НАСТРОЙКИ СЛОЕВ UNITY ===")]
    public string coreLayerName = "Core";
    public string middleLayerName = "Middle";
    public string surfaceLayerName = "Surface";

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

    // Новые системы
    private List<SlimeNodeInfo> nodeHierarchy = new List<SlimeNodeInfo>();
    private float slideTimer = 0f;
    private const float SLIDE_UPDATE_INTERVAL = 0.1f;
    private int coreLayer;
    private int middleLayer;
    private int surfaceLayer;

    // Система вращения
    private Dictionary<Rigidbody2D, float> currentRotationAngles = new Dictionary<Rigidbody2D, float>();
    private Dictionary<Rigidbody2D, float> targetRotationAngles = new Dictionary<Rigidbody2D, float>();
    private Dictionary<Rigidbody2D, float> rotationUpdateTimes = new Dictionary<Rigidbody2D, float>();

    // Система телепортации
    private Dictionary<Rigidbody2D, float> lastTeleportTime = new Dictionary<Rigidbody2D, float>();
    private Dictionary<Rigidbody2D, bool> nodeTeleportStatus = new Dictionary<Rigidbody2D, bool>();

    // Система вытягивания
    private bool isStretching = false;
    private float currentStretchProgress = 0f;
    private float originalMoveSpeed;
    private float originalJumpSpeed;

    // Система сползания
    private float currentSlopeAngle = 0f;

    // Система фиксации позиции
    private bool isPositionLocked = false;
    private Vector2 lockedPosition;

    [System.Serializable]
    public class SlimeNodeInfo
    {
        public Rigidbody2D node;
        public int layerIndex;
        public List<Rigidbody2D> influencedNodes;
        public Rigidbody2D masterNode;
        public Vector2 restPosition;
        public float currentRotation;
        public Vector2 connectionPoint;
    }

    void Start()
    {
        coreLayer = LayerMask.NameToLayer(coreLayerName);
        middleLayer = LayerMask.NameToLayer(middleLayerName);
        surfaceLayer = LayerMask.NameToLayer(surfaceLayerName);

        CreateCharacter();
        SetupOscillation();
        InitializeInertia();
        originalGravityScale = gravityMultiplier;
        originalMoveSpeed = moveSpeed;
        originalJumpSpeed = jumpInitialSpeed;

        InitializeNodeHierarchy();
        InitializeRotationSystem();
        InitializeTeleportSystem();
    }

    void CreateCharacter()
    {
        CreateCenterBody();
        CreateNodeLayers();
        ConnectNodeLayers(); // УПРОЩЕННАЯ СИСТЕМА СОЕДИНЕНИЙ
        CalculateRestPositions();
        InitializeStuckTracking();
    }

    void CreateCenterBody()
    {
        centerBody = GetComponent<Rigidbody2D>();
        if (centerBody == null)
            centerBody = gameObject.AddComponent<Rigidbody2D>();

        centerBody.gravityScale = 0;
        centerBody.linearDamping = 2f;
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
        CreateNodeLayer(0, "Core", coreNodes, coreNodesCount, coreDrag, 0.05f, 0.3f, coreLayer);
        CreateNodeLayer(1, "Middle", middleNodes, middleNodesCount, 1.5f, 0.07f, 0.6f, middleLayer);
        CreateNodeLayer(2, "Surface", surfaceNodes, surfaceNodesCount, surfaceDrag * 1.5f, 0.09f, 1f, surfaceLayer);
    }

    void CreateNodeLayer(int layerIndex, string prefix, List<Rigidbody2D> nodes, int count, float drag, float colliderSize, float gravityEffect, int unityLayer)
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
            node.layer = unityLayer;

            Rigidbody2D rb = node.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravityMultiplier * gravityEffect;
            rb.linearDamping = drag;
            rb.angularDamping = drag * 0.8f;
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

    private void InitializeNodeHierarchy()
    {
        nodeHierarchy.Clear();

        foreach (var node in coreNodes)
        {
            if (node != null)
            {
                nodeHierarchy.Add(new SlimeNodeInfo
                {
                    node = node,
                    layerIndex = 0,
                    influencedNodes = new List<Rigidbody2D>(),
                    masterNode = null,
                    restPosition = GetRestPositionForNode(node, 0),
                    currentRotation = 0f,
                    connectionPoint = node.position
                });
            }
        }

        foreach (var node in middleNodes)
        {
            if (node != null)
            {
                Rigidbody2D closestCore = FindClosestNodeInLayer(node.position, coreNodes);

                nodeHierarchy.Add(new SlimeNodeInfo
                {
                    node = node,
                    layerIndex = 1,
                    influencedNodes = new List<Rigidbody2D>(),
                    masterNode = closestCore,
                    restPosition = GetRestPositionForNode(node, 1),
                    currentRotation = Random.Range(0f, 360f),
                    connectionPoint = CalculateConnectionPoint(closestCore.position, node.position, 1)
                });

                var coreInfo = nodeHierarchy.Find(n => n.node == closestCore);
                if (coreInfo != null)
                {
                    coreInfo.influencedNodes.Add(node);
                }
            }
        }

        foreach (var node in surfaceNodes)
        {
            if (node != null)
            {
                Rigidbody2D closestMiddle = FindClosestNodeInLayer(node.position, middleNodes);

                nodeHierarchy.Add(new SlimeNodeInfo
                {
                    node = node,
                    layerIndex = 2,
                    influencedNodes = new List<Rigidbody2D>(),
                    masterNode = closestMiddle,
                    restPosition = GetRestPositionForNode(node, 2),
                    currentRotation = 0f,
                    connectionPoint = CalculateConnectionPoint(closestMiddle.position, node.position, 2)
                });

                var middleInfo = nodeHierarchy.Find(n => n.node == closestMiddle);
                if (middleInfo != null)
                {
                    middleInfo.influencedNodes.Add(node);
                }
            }
        }
    }

    private Vector2 CalculateConnectionPoint(Vector2 masterPos, Vector2 nodePos, int layerIndex)
    {
        float connectionDistance = layerDistances[layerIndex] * 0.7f;
        Vector2 direction = (nodePos - masterPos).normalized;
        return masterPos + direction * connectionDistance;
    }

    private Vector2 GetRestPositionForNode(Rigidbody2D node, int layerIndex)
    {
        Vector2 toNode = node.position - (Vector2)transform.position;
        float distance = toNode.magnitude;
        float targetDistance = layerDistances[layerIndex];

        if (distance > 0.1f)
        {
            return toNode.normalized * targetDistance;
        }
        else
        {
            return Random.insideUnitCircle.normalized * targetDistance;
        }
    }

    private void InitializeRotationSystem()
    {
        foreach (var node in middleNodes)
        {
            if (node != null)
            {
                currentRotationAngles[node] = Random.Range(0f, 360f);
                targetRotationAngles[node] = currentRotationAngles[node];
                rotationUpdateTimes[node] = Time.time;
            }
        }
    }

    private void InitializeTeleportSystem()
    {
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                lastTeleportTime[node] = 0f;
                nodeTeleportStatus[node] = false;
            }
        }
    }

    private Rigidbody2D FindClosestNodeInLayer(Vector2 position, List<Rigidbody2D> layerNodes)
    {
        Rigidbody2D closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var node in layerNodes)
        {
            if (node == null) continue;

            float distance = Vector2.Distance(position, node.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = node;
            }
        }

        return closest;
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

    // УПРОЩЕННАЯ СИСТЕМА СОЕДИНЕНИЙ - УБРАЛ СВЯЗИ МЕЖДУ CORE И MIDDLE
    void ConnectNodeLayers()
    {
        // Каждый слой соединяется только с центром
        ConnectLayerToCenter(coreNodes, 0, coreStiffness);
        ConnectLayerToCenter(middleNodes, 1, middleStiffness);
        ConnectLayerToCenter(surfaceNodes, 2, surfaceStiffness);

        // Соединения внутри слоев остаются
        ConnectNodesInLayer(coreNodes, coreStiffness * 2f, 0.9f);
        ConnectNodesInLayer(middleNodes, middleStiffness * 1.5f, 0.8f);
        ConnectNodesInLayer(surfaceNodes, surfaceStiffness * 1.2f, 0.7f);

        // УБРАЛ: ConnectAdjacentLayers(coreNodes, middleNodes, 0.8f, middleStiffness * 1.5f);

        // Оставляем только связь между middle и surface
        ConnectAdjacentLayers(middleNodes, surfaceNodes, 0.7f, middleStiffness * 1.2f);
    }

    void ConnectLayerToCenter(List<Rigidbody2D> nodes, int layerIndex, float stiffness)
    {
        float distance = layerDistances[layerIndex];
        foreach (Rigidbody2D node in nodes)
        {
            SpringJoint2D joint = node.gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = centerBody;
            joint.distance = distance;
            joint.dampingRatio = 0.9f;
            joint.frequency = stiffness / 120f;
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
            joint.frequency = stiffness / 120f;
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
            joint.dampingRatio = 0.7f;
            joint.frequency = stiffness / 150f;
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

        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            StartJump();
        }

        if (Input.GetKey(KeyCode.W))
        {
            isHoldingW = true;
        }
        else
        {
            isHoldingW = false;
        }

        if (Input.GetKey(KeyCode.S))
        {
            isHoldingS = true;
            if (!isJumping && !IsOnGround())
            {
                ApplyFastFall();
            }
        }
        else
        {
            isHoldingS = false;
        }

        CheckPositionLock();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        HandleJump();
        HandleStretching();
        ApplyLiquidBehavior();
        StabilizeCoreLayer();
        StabilizeMiddleLayer();
        HandleObstacleClimbing();
        PreventNodeSticking();
        HandleStuckNodes();
        ApplyCorePriority();
        ApplyGroundFriction();
        ApplyLayerInertia();
        LimitNodeVelocities();

        ApplySurfaceSliding();
        ApplyAutoClimbing();
        ApplyHierarchicalForces(); // УСИЛЕННАЯ СИСТЕМА ДЛЯ КОМПЕНСАЦИИ
        ApplyFreeRotation();
        HandleTeleportThroughObstacles();

        ApplyStabilizationForces();
        HandleSlopeSliding();
        ApplyPositionLock();
    }

    void GetPlayerInput()
    {
        moveInput = Vector2.zero;

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
        if (moveInput.magnitude > 0.1f && !isPositionLocked)
        {
            float currentMoveSpeed = isStretching ? originalMoveSpeed * stretchMoveSpeedMultiplier : originalMoveSpeed;
            Vector2 targetVelocity = moveInput * currentMoveSpeed;
            Vector2 velocityChange = targetVelocity - centerBody.linearVelocity;
            centerBody.AddForce(velocityChange * 20f);
        }
    }

    void ApplyGroundFriction()
    {
        if (IsOnGround() && moveInput.magnitude < 0.1f && centerBody.linearVelocity.magnitude < 0.5f)
        {
            centerBody.linearVelocity = Vector2.Lerp(centerBody.linearVelocity, Vector2.zero, groundFriction * Time.fixedDeltaTime);
        }
    }

    void ApplyLayerInertia()
    {
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

    private void CheckPositionLock()
    {
        bool shouldLock = IsOnGround() &&
                         moveInput.magnitude < 0.05f &&
                         centerBody.linearVelocity.magnitude < positionLockThreshold &&
                         !isJumping && !isStretching;

        if (shouldLock && !isPositionLocked)
        {
            isPositionLocked = true;
            lockedPosition = centerBody.position;
        }
        else if (!shouldLock && isPositionLocked)
        {
            isPositionLocked = false;
        }
    }

    private void ApplyPositionLock()
    {
        if (isPositionLocked)
        {
            Vector2 positionError = lockedPosition - centerBody.position;
            if (positionError.magnitude > 0.05f)
            {
                centerBody.AddForce(positionError * positionLockForce);
            }

            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    Vector2 toCenter = (Vector2)transform.position - node.position;
                    float distance = toCenter.magnitude;
                    float targetDistance = GetTargetDistanceForNode(node);

                    if (Mathf.Abs(distance - targetDistance) > 0.1f)
                    {
                        Vector2 targetPos = (Vector2)transform.position + toCenter.normalized * targetDistance;
                        Vector2 correction = (targetPos - node.position) * positionLockForce * 0.3f;
                        node.AddForce(correction);
                    }
                }
            }
        }
    }

    private float GetTargetDistanceForNode(Rigidbody2D node)
    {
        if (coreNodes.Contains(node)) return layerDistances[0];
        if (middleNodes.Contains(node)) return layerDistances[1];
        if (surfaceNodes.Contains(node)) return layerDistances[2];
        return 1f;
    }

    private void HandleStretching()
    {
        bool wantsToStretch = Input.GetKey(KeyCode.W);
        bool canStretch = IsOnGround() && GetGroundContactPercentage() >= minGroundContactPercent;

        if (wantsToStretch && canStretch && !isJumping)
        {
            if (!isStretching)
            {
                isStretching = true;
                currentStretchProgress = 0f;
                moveSpeed = originalMoveSpeed * stretchMoveSpeedMultiplier;
                jumpInitialSpeed = originalJumpSpeed * stretchJumpMultiplier;
            }

            currentStretchProgress = Mathf.MoveTowards(currentStretchProgress, 1f, stretchReturnSpeed * Time.fixedDeltaTime);
            ApplyStretchForces();
        }
        else
        {
            if (isStretching)
            {
                currentStretchProgress = Mathf.MoveTowards(currentStretchProgress, 0f, stretchReturnSpeed * Time.fixedDeltaTime);

                if (currentStretchProgress <= 0.01f)
                {
                    isStretching = false;
                    moveSpeed = originalMoveSpeed;
                    jumpInitialSpeed = originalJumpSpeed;
                }
                else
                {
                    ApplyStretchForces();
                }
            }
        }
    }

    private void ApplyStretchForces()
    {
        if (!isStretching && currentStretchProgress <= 0.01f) return;

        var upperNodes = surfaceNodes.Where(node =>
            node != null && node.position.y > centerBody.position.y).ToList();

        float upwardForce = stretchUpwardForce * currentStretchProgress;
        float horizontalForce = stretchHorizontalForce * currentStretchProgress;

        foreach (var node in upperNodes)
        {
            Vector2 stretchDirection = (Vector2.up + Random.insideUnitCircle * 0.2f).normalized;
            node.AddForce(stretchDirection * upwardForce);

            if (Mathf.Abs(node.position.x - centerBody.position.x) > layerDistances[2] * 0.5f)
            {
                Vector2 toCenterX = new Vector2(centerBody.position.x - node.position.x, 0).normalized;
                node.AddForce(toCenterX * horizontalForce * 0.3f);
            }
        }

        foreach (var node in middleNodes)
        {
            if (node != null && node.position.y > centerBody.position.y)
            {
                Vector2 stretchDirection = (Vector2.up + Random.insideUnitCircle * 0.1f).normalized;
                node.AddForce(stretchDirection * upwardForce * 0.6f);
            }
        }
    }

    private void ApplyStabilizationForces()
    {
        if (IsOnGround() && moveInput.magnitude < 0.1f && centerBody.linearVelocity.magnitude < velocityStoppingThreshold)
        {
            Vector2 frictionForce = -centerBody.linearVelocity.normalized * staticFrictionForce;
            centerBody.AddForce(frictionForce);

            StabilizeGroundNodes();

            if (centerBody.linearVelocity.magnitude < 0.1f)
            {
                centerBody.linearVelocity = Vector2.zero;
            }
        }
    }

    private void StabilizeGroundNodes()
    {
        foreach (var surfaceNode in surfaceNodes)
        {
            if (surfaceNode == null) continue;

            RaycastHit2D hit = Physics2D.Raycast(surfaceNode.position, Vector2.down, 0.3f, obstacleLayers);
            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                surfaceNode.AddForce(Vector2.down * groundStabilizationForce * 0.5f);
            }
        }
    }

    private void HandleSlopeSliding()
    {
        if (!IsOnGround()) return;

        CalculateCurrentSlopeAngle();

        if (currentSlopeAngle > maxStableSlopeAngle && moveInput.magnitude < slopeStabilizationThreshold)
        {
            Vector2 slideDirection = GetSlopeDirection();
            foreach (var node in surfaceNodes)
            {
                if (node != null)
                {
                    node.AddForce(slideDirection * slideDownForce * (currentSlopeAngle / 90f));
                }
            }
            centerBody.AddForce(slideDirection * slideDownForce * 0.5f * (currentSlopeAngle / 90f));
        }
    }

    private void CalculateCurrentSlopeAngle()
    {
        RaycastHit2D[] hits = new RaycastHit2D[5];
        int hitCount = centerBody.Cast(Vector2.down, hits, 1f);

        float maxAngle = 0f;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].collider.CompareTag("Ground"))
            {
                float angle = Vector2.Angle(hits[i].normal, Vector2.up);
                if (angle > maxAngle) maxAngle = angle;
            }
        }

        currentSlopeAngle = maxAngle;
    }

    private Vector2 GetSlopeDirection()
    {
        RaycastHit2D hit = Physics2D.Raycast(centerBody.position, Vector2.down, 1f, obstacleLayers);
        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            return new Vector2(-hit.normal.y, hit.normal.x).normalized;
        }
        return Vector2.right;
    }

    private void ApplyFreeRotation()
    {
        if (!enableFreeRotation) return;

        foreach (var middleNode in middleNodes)
        {
            if (middleNode == null) continue;

            var nodeInfo = nodeHierarchy.Find(n => n.node == middleNode);
            if (nodeInfo == null || nodeInfo.masterNode == null) continue;

            if (Time.time - rotationUpdateTimes[middleNode] > 0.2f)
            {
                UpdateTargetRotation(nodeInfo);
                rotationUpdateTimes[middleNode] = Time.time;
            }

            currentRotationAngles[middleNode] = Mathf.LerpAngle(
                currentRotationAngles[middleNode],
                targetRotationAngles[middleNode],
                rotationReturnForce * Time.fixedDeltaTime
            );

            ApplyRotationThroughConnection(nodeInfo);
        }
    }

    private void UpdateTargetRotation(SlimeNodeInfo nodeInfo)
    {
        Vector2 movement = GetMovementDirection();
        float movementInfluence = Vector2.Dot(movement, (nodeInfo.node.position - nodeInfo.masterNode.position).normalized);

        targetRotationAngles[nodeInfo.node] = currentRotationAngles[nodeInfo.node] +
            Random.Range(-rotationFreedom, rotationFreedom) * 0.1f +
            movementInfluence * rotationFreedom * 0.3f;
    }

    private void ApplyRotationThroughConnection(SlimeNodeInfo nodeInfo)
    {
        float currentAngle = currentRotationAngles[nodeInfo.node] * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * layerDistances[1] * 0.7f;

        Vector2 targetConnectionPoint = nodeInfo.masterNode.position + offset;
        nodeInfo.connectionPoint = targetConnectionPoint;

        Vector2 toConnection = targetConnectionPoint - nodeInfo.node.position;
        float distance = toConnection.magnitude;

        if (distance > 0.1f)
        {
            nodeInfo.node.AddForce(toConnection.normalized * distance * coreToMiddleForce);
        }

        Debug.DrawLine(nodeInfo.masterNode.position, targetConnectionPoint, Color.cyan);
        Debug.DrawLine(targetConnectionPoint, nodeInfo.node.position, Color.yellow);
    }

    // ИСПРАВЛЕННЫЕ МЕТОДЫ

    private bool IsPathToCenterBlocked(Vector2 nodePosition)
    {
        Vector2 toCenter = (Vector2)transform.position - nodePosition;
        float distance = toCenter.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(nodePosition, toCenter.normalized, distance, obstacleLayers);
        return hit.collider != null && hit.collider.CompareTag("Ground");
    }

    private Vector2 GetTangentialDirection(Vector2 nodePos, Vector2 centerPos)
    {
        Vector2 toCenter = centerPos - nodePos;
        if (toCenter.magnitude < 0.01f)
            return Vector2.right;

        return new Vector2(-toCenter.y, toCenter.x).normalized;
    }

    private Vector2 FindClearPathPosition(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float maxDistance = Vector2.Distance(from, to);

        for (float dist = maxDistance * 0.3f; dist < maxDistance; dist += 0.2f)
        {
            Vector2 testPoint = from + direction * dist;
            if (!Physics2D.OverlapCircle(testPoint, 0.3f, obstacleLayers))
            {
                return testPoint;
            }
        }

        return from + direction * Mathf.Max(maxDistance * 0.7f, minTeleportDistance);
    }

    private SlidingSolution CheckSurfaceSliding(Rigidbody2D node)
    {
        if (node == null) return SlidingSolution.None;

        Vector2 nodePos = node.position;

        RaycastHit2D hitDown = Physics2D.Raycast(nodePos, Vector2.down, slideDetectionRange, obstacleLayers);
        RaycastHit2D hitForward = Physics2D.Raycast(nodePos, GetMovementDirection(), slideDetectionRange, obstacleLayers);
        RaycastHit2D hitUp = Physics2D.Raycast(nodePos + Vector2.up * 0.2f, GetMovementDirection(), slideDetectionRange * 0.8f, obstacleLayers);

        if (hitDown.collider != null && hitDown.collider.CompareTag("Ground"))
        {
            float surfaceAngle = Vector2.Angle(hitDown.normal, Vector2.up);

            if (surfaceAngle < 30f)
                return SlidingSolution.HorizontalSlide;
            else if (surfaceAngle < maxSlideAngle)
                return SlidingSolution.CornerSlide;
        }

        if (hitForward.collider != null && hitUp.collider == null)
            return SlidingSolution.VerticalClimb;

        return SlidingSolution.None;
    }

    private bool CanAutoClimb(Rigidbody2D node)
    {
        if (node == null) return false;

        Vector2 nodePos = node.position;
        Vector2 movementDir = GetMovementDirection();

        RaycastHit2D hitForward = Physics2D.Raycast(nodePos, movementDir, climbDetectionRange, obstacleLayers);
        if (hitForward.collider == null || !hitForward.collider.CompareTag("Ground"))
            return false;

        Vector2 climbStart = nodePos + movementDir * climbDetectionRange * 0.5f;
        RaycastHit2D hitUp = Physics2D.Raycast(climbStart, Vector2.up, maxAutoClimbHeight, obstacleLayers);

        return hitUp.collider == null;
    }

    private float GetMaxDistanceForLayer(List<Rigidbody2D> nodes)
    {
        if (nodes == coreNodes) return layerDistances[0];
        if (nodes == middleNodes) return layerDistances[1];
        if (nodes == surfaceNodes) return layerDistances[2];
        return 1f;
    }

    // Остальные методы остаются без изменений...

    void ApplyFastFall()
    {
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

            currentJumpSpeed = jumpInitialSpeed / Mathf.Max(0.5f, currentMass);

            if (isHoldingS)
            {
                currentJumpSpeed *= 1.5f;
            }

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
                float jumpForce = currentJumpSpeed * Mathf.Pow(jumpDecayRate, jumpTimer * 10f);

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
        foreach (Rigidbody2D node in coreNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 0.3f;
        foreach (Rigidbody2D node in middleNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 0.6f;
        foreach (Rigidbody2D node in surfaceNodes)
            if (node != null) node.gravityScale = gravityMultiplier * 1f;
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
        if (moveInput.magnitude > 0.1f)
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

    void StabilizeCoreLayer()
    {
        for (int i = 0; i < coreNodes.Count; i++)
        {
            if (coreNodes[i] == null) continue;

            Vector2 targetPos = (Vector2)transform.position + nodeRestPositions[i];
            Vector2 toTarget = targetPos - (Vector2)coreNodes[i].position;
            float distance = toTarget.magnitude;

            if (distance > 0.05f)
            {
                float forceMultiplier = distance * 120f;
                coreNodes[i].AddForce(toTarget.normalized * forceMultiplier);
            }
        }
    }

    void StabilizeMiddleLayer()
    {
        for (int i = 0; i < middleNodes.Count; i++)
        {
            if (middleNodes[i] == null) continue;

            Vector2 targetPos = (Vector2)transform.position + nodeRestPositions[coreNodesCount + i];
            Vector2 toTarget = targetPos - (Vector2)middleNodes[i].position;
            float distance = toTarget.magnitude;

            if (distance > 0.1f)
            {
                middleNodes[i].AddForce(toTarget.normalized * distance * 60f);
            }
        }
    }

    void HandleObstacleClimbing()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector2 checkDir = moveInput.normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, checkDir, obstacleDetectionRange, obstacleLayers);

            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                RaycastHit2D topHit = Physics2D.Raycast(
                    (Vector2)transform.position + checkDir * obstacleDetectionRange,
                    Vector2.up, 0.5f, obstacleLayers);

                if (topHit.collider == null)
                {
                    centerBody.AddForce((Vector2.up * 0.7f + checkDir * 0.3f) * climbAssistForce);

                    foreach (Rigidbody2D node in surfaceNodes)
                    {
                        if (node == null) continue;
                        node.AddForce((Vector2.up * 0.5f + checkDir * 0.2f) * climbAssistForce * 0.4f);
                    }
                }
                else
                {
                    Vector2 flowDirection = new Vector2(-checkDir.y, checkDir.x);
                    foreach (Rigidbody2D node in surfaceNodes)
                    {
                        if (node == null) continue;

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

    void HandleStuckNodes()
    {
        if (Time.time - lastUnstuckCheck < 0.5f) return;
        lastUnstuckCheck = Time.time;

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

            bool isStuck = distanceFromCenter > maxAllowedDistance &&
                          nodes[i].linearVelocity.magnitude < 0.1f;

            if (isStuck && !nodeStuckStatus[globalIndex])
            {
                nodeStuckStatus[globalIndex] = true;
                UnstuckNode(nodes[i]);
            }
            else if (!isStuck && nodeStuckStatus[globalIndex])
            {
                nodeStuckStatus[globalIndex] = false;
            }
        }
    }

    void UnstuckNode(Rigidbody2D node)
    {
        Vector2 toCenter = (centerBody.position - node.position).normalized;
        Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
        Vector2 unstuckDirection = (toCenter + randomOffset).normalized;

        node.AddForce(unstuckDirection * unstuckForce, ForceMode2D.Impulse);
        node.MovePosition(node.position + unstuckDirection * 0.2f);
    }

    private void HandleTeleportThroughObstacles()
    {
        if (!enableTeleportThroughObstacles) return;

        foreach (var node in allNodes)
        {
            if (node == null) continue;

            float distanceToCenter = Vector2.Distance(node.position, transform.position);

            bool isTooFar = distanceToCenter > teleportThreshold;
            bool isStuck = node.linearVelocity.magnitude < 0.5f;
            bool canTeleport = Time.time - lastTeleportTime[node] > teleportCooldown;
            bool isPathBlocked = IsPathToCenterBlocked(node.position);

            if (isTooFar && isStuck && canTeleport && isPathBlocked && !nodeTeleportStatus[node])
            {
                SmoothTeleportNode(node);
            }
            else if (distanceToCenter < teleportThreshold * 0.7f)
            {
                nodeTeleportStatus[node] = false;
            }
        }
    }

    private void SmoothTeleportNode(Rigidbody2D node)
    {
        Vector2 toCenter = (Vector2)transform.position - node.position;
        float distance = toCenter.magnitude;

        if (distance < minTeleportDistance) return;

        Vector2 teleportPosition = FindClearPathPosition(node.position, transform.position);

        node.MovePosition(Vector2.Lerp(node.position, teleportPosition, 0.7f));
        node.AddForce(toCenter.normalized * teleportForce * 0.5f, ForceMode2D.Impulse);

        node.linearVelocity *= 0.3f;
        node.angularVelocity = 0f;

        lastTeleportTime[node] = Time.time;
        nodeTeleportStatus[node] = true;

        Debug.Log($"Smooth teleported node {node.name}");
        Debug.DrawLine(node.position, teleportPosition, Color.red, 2f);
    }

    private void ApplySurfaceSliding()
    {
        if (!enableSurfaceSliding) return;

        slideTimer += Time.fixedDeltaTime;
        if (slideTimer < SLIDE_UPDATE_INTERVAL) return;
        slideTimer = 0f;

        foreach (var surfaceNode in surfaceNodes)
        {
            if (surfaceNode == null) continue;

            SlidingSolution slideSolution = CheckSurfaceSliding(surfaceNode);

            switch (slideSolution)
            {
                case SlidingSolution.HorizontalSlide:
                    ApplyHorizontalSliding(surfaceNode);
                    break;
                case SlidingSolution.VerticalClimb:
                    ApplyVerticalSliding(surfaceNode);
                    break;
                case SlidingSolution.CornerSlide:
                    ApplyCornerSliding(surfaceNode);
                    break;
            }
        }
    }

    private void ApplyHorizontalSliding(Rigidbody2D node)
    {
        Vector2 slideDirection = GetMovementDirection();
        node.AddForce(slideDirection * surfaceSlideForce * slideAcceleration);
        Debug.DrawRay(node.position, slideDirection * 0.5f, Color.blue);
    }

    private void ApplyVerticalSliding(Rigidbody2D node)
    {
        Vector2 climbDirection = (Vector2.up + GetMovementDirection() * 0.3f).normalized;
        node.AddForce(climbDirection * surfaceSlideForce * climbAssistMultiplier);
        Debug.DrawRay(node.position, climbDirection * 0.6f, Color.green);
    }

    private void ApplyCornerSliding(Rigidbody2D node)
    {
        RaycastHit2D hit = Physics2D.Raycast(node.position, Vector2.down, slideDetectionRange, obstacleLayers);
        if (hit.collider != null)
        {
            Vector2 surfaceNormal = hit.normal;
            Vector2 slideDirection = new Vector2(-surfaceNormal.y, surfaceNormal.x);

            Vector2 movementDir = GetMovementDirection();
            if (Vector2.Dot(slideDirection, movementDir) < 0)
                slideDirection = -slideDirection;

            node.AddForce(slideDirection * surfaceSlideForce);
            Debug.DrawRay(node.position, slideDirection * 0.5f, Color.yellow);
        }
    }

    private void ApplyAutoClimbing()
    {
        if (!enableSurfaceSliding) return;

        foreach (var node in allNodes)
        {
            if (node == null) continue;

            if (CanAutoClimb(node))
            {
                ApplyClimbingForce(node);
            }
        }
    }

    private void ApplyClimbingForce(Rigidbody2D node)
    {
        Vector2 climbDirection = (Vector2.up + GetMovementDirection() * 0.5f).normalized;

        SlimeNodeBehavior nodeBehavior = node.GetComponent<SlimeNodeBehavior>();
        float forceMultiplier = nodeBehavior != null && nodeBehavior.layerIndex == 2 ? 1f : 0.7f;

        node.AddForce(climbDirection * autoClimbForce * forceMultiplier);
        Debug.DrawRay(node.position, climbDirection * 0.8f, Color.magenta);
    }

    // УСИЛЕННАЯ СИСТЕМА ИЕРАРХИЧЕСКИХ СИЛ ДЛЯ КОМПЕНСАЦИИ ОТСУТСТВИЯ ПРУЖИН
    private void ApplyHierarchicalForces()
    {
        if (!enableHierarchicalForces) return;

        foreach (var nodeInfo in nodeHierarchy)
        {
            if (nodeInfo.node == null) continue;

            if (nodeInfo.layerIndex == 0)
            {
                ApplyMasterInfluence(nodeInfo);
            }
            else if (nodeInfo.layerIndex == 1)
            {
                // УСИЛЕННОЕ ВЛИЯНИЕ НА СРЕДНИЕ УЗЛЫ
                ApplyMasterInfluence(nodeInfo);

                if (nodeInfo.masterNode != null)
                {
                    ApplyFollowerBehavior(nodeInfo.node, nodeInfo.masterNode, coreToMiddleForce * 1.5f);
                }
            }
            else if (nodeInfo.layerIndex == 2 && nodeInfo.masterNode != null)
            {
                ApplyFollowerBehavior(nodeInfo.node, nodeInfo.masterNode, middleToSurfaceForce);
            }
        }
    }

    private void ApplyMasterInfluence(SlimeNodeInfo masterInfo)
    {
        if (masterInfo.influencedNodes == null) return;

        foreach (var influencedNode in masterInfo.influencedNodes)
        {
            if (influencedNode == null) continue;

            float distance = Vector2.Distance(masterInfo.node.position, influencedNode.position);
            if (distance > hierarchicalInfluenceRadius) continue;

            float force = masterInfo.layerIndex == 0 ? coreToMiddleForce : middleToSurfaceForce;
            force = Mathf.Min(force * (distance / hierarchicalInfluenceRadius), maxHierarchicalForce);

            Vector2 direction = (masterInfo.node.position - influencedNode.position).normalized;
            influencedNode.AddForce(direction * force);

            if (masterInfo.layerIndex == 0)
                Debug.DrawLine(masterInfo.node.position, influencedNode.position, Color.red);
            else
                Debug.DrawLine(masterInfo.node.position, influencedNode.position, Color.yellow);
        }
    }

    private void ApplyFollowerBehavior(Rigidbody2D follower, Rigidbody2D master, float baseForce)
    {
        float distance = Vector2.Distance(follower.position, master.position);
        if (distance > hierarchicalInfluenceRadius) return;

        float force = Mathf.Min(baseForce * (distance / hierarchicalInfluenceRadius), maxHierarchicalForce);
        Vector2 direction = (master.position - follower.position).normalized;
        follower.AddForce(direction * force);
    }

    private Vector2 GetMovementDirection()
    {
        Vector2 centerVelocity = GetCenterVelocity();
        return centerVelocity.magnitude > 0.1f ? centerVelocity.normalized : Vector2.right;
    }

    private List<Rigidbody2D> allNodes
    {
        get
        {
            var all = new List<Rigidbody2D>();
            all.AddRange(coreNodes);
            all.AddRange(middleNodes);
            all.AddRange(surfaceNodes);
            return all;
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

    public void ReportNodeStuck(Rigidbody2D node)
    {
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

    public Vector2 GetCenterVelocity()
    {
        return centerBody.linearVelocity;
    }

    public bool IsStretching()
    {
        return isStretching;
    }

    public bool IsJumping()
    {
        return isJumping;
    }

    public float GetGroundContactPercentage()
    {
        return (float)surfaceGroundContactCount / surfaceNodesCount * 100f;
    }

    public float GetCurrentSlopeAngle()
    {
        return currentSlopeAngle;
    }

    public bool IsPositionLocked()
    {
        return isPositionLocked;
    }

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

    private void OnDrawGizmosSelected()
    {
        if (!enableTeleportThroughObstacles) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, teleportThreshold);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minTeleportDistance);

        foreach (var node in allNodes)
        {
            if (node == null) continue;

            float distance = Vector2.Distance(node.position, transform.position);
            if (distance > teleportThreshold && node.linearVelocity.magnitude < 0.3f)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(node.position, 0.2f);
            }
        }

        Gizmos.color = currentSlopeAngle > maxStableSlopeAngle ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position, GetSlopeDirection() * 1f);

        if (isPositionLocked)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(lockedPosition, 0.2f);
        }
    }
}

public enum SlidingSolution
{
    None,
    HorizontalSlide,
    VerticalClimb,
    CornerSlide
}