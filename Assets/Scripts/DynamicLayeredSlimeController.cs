using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SlimeCharacterController : MonoBehaviour
{
    [Header("Layer Configuration")]
    public int coreNodesCount = 6;
    public int middleNodesCount = 10;
    public int surfaceNodesCount = 14;
    public float[] layerDistances = new float[] { 0.5f, 0.9f, 1.3f };

    [Header("Layer Properties")]
    public float coreStiffness = 1200f;
    public float middleStiffness = 400f;
    public float surfaceStiffness = 60f;
    public float coreDrag = 3f;
    public float surfaceDrag = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpPower = 16f;
    public float gravityMultiplier = 2.5f;
    public float climbAssistForce = 12f;
    public float obstacleDetectionRange = 0.7f;
    public float corePriorityForce = 15f;
    public float groundFriction = 10f;

    [Header("Liquid Behavior")]
    public float surfaceOscillationForce = 2f;
    public float surfaceFluidity = 0.3f;
    public float maxSurfaceWobble = 0.4f;

    [Header("Collision Prevention")]
    public float nodeRepelForce = 50f;
    public float unstuckForce = 100f;
    public float maxUnstuckDistance = 2f;
    public LayerMask obstacleLayers = 1;

    [Header("Dynamic Properties")]
    public float currentMass = 1f;
    public float currentSize = 1f;
    public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);

    // Node collections
    public List<Rigidbody2D> coreNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> middleNodes { get; private set; } = new List<Rigidbody2D>();
    public List<Rigidbody2D> surfaceNodes { get; private set; } = new List<Rigidbody2D>();

    private Rigidbody2D centerBody;
    private bool isOnGround = false;
    private Vector2 moveInput;
    private List<Vector2> nodeRestPositions = new List<Vector2>();
    private List<float> surfaceOscillationOffsets = new List<float>();
    private float oscillationTimer = 0f;
    private List<bool> nodeStuckStatus = new List<bool>();
    private float lastUnstuckCheck;
    private bool jumpRequested = false;
    private float lastJumpTime;
    private bool isJumping = false;
    private Vector2 previousVelocity;

    void Start()
    {
        InitializeTags();
        CreateCharacter();
        SetupOscillation();
    }

    void InitializeTags()
    {
        CreateTagIfMissing("SlimeNode");
        CreateTagIfMissing("Ground");
        CreateTagIfMissing("Obstacle");
    }

    void CreateTagIfMissing(string tagName)
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
        centerBody.linearDamping = 1.5f; // Increased drag for less sliding
        centerBody.angularDamping = 2f;
        centerBody.mass = 2f * currentMass;
        centerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Add center collider for obstacle detection
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
            Vector2 position = (Vector2)transform.position + direction * radius;

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

            CircleCollider2D collider = node.AddComponent<CircleCollider2D>();
            collider.radius = colliderSize;

            // Add node behavior script for stuck detection
            SlimeNodeBehavior nodeBehavior = node.AddComponent<SlimeNodeBehavior>();
            nodeBehavior.Initialize(this, centerBody);

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
        if (Input.GetKeyDown(KeyCode.Space) && isOnGround)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        HandleJump();
        ApplyLiquidBehavior();
        StabilizeCoreLayer();
        HandleObstacleClimbing();
        PreventNodeSticking();
        HandleStuckNodes();
        ApplyCorePriority();
        ApplyGroundFriction();

        // Store velocity for next frame
        previousVelocity = centerBody.linearVelocity;
    }

    void GetPlayerInput()
    {
        moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
        if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
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
        if (isOnGround && moveInput.magnitude < 0.1f)
        {
            // Reduce horizontal velocity
            Vector2 velocity = centerBody.linearVelocity;
            velocity.x *= 0.9f; // Reduce horizontal speed by 10% each frame

            // If velocity is very small, stop completely
            if (Mathf.Abs(velocity.x) < 0.1f)
            {
                velocity.x = 0f;
            }

            centerBody.linearVelocity = velocity;
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

    void HandleJump()
    {
        if (jumpRequested && isOnGround)
        {
            // Apply impulse directly to center body
            centerBody.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            lastJumpTime = Time.time;

            isOnGround = false;
            isJumping = true;
            jumpRequested = false;

            // Start coroutine to reset jumping state
            StartCoroutine(ResetJumpState());
        }
    }

    private IEnumerator ResetJumpState()
    {
        yield return new WaitForSeconds(0.5f);
        isJumping = false;
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

            if (hit.collider != null && (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Obstacle")))
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
                if (obstacle.CompareTag("Ground") || obstacle.CompareTag("Obstacle"))
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isOnGround = false;
        }
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
}