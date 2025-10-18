using UnityEngine;

public class SlimeNodeBehavior : MonoBehaviour
{
    private SlimeCharacterController slimeController;
    private Rigidbody2D nodeRigidbody;
    private Rigidbody2D centerBody;
    private float lastStuckCheck;
    private bool isStuck = false;
    private float stuckTimer = 0f;
    private bool isGrounded = false;
    private int layerIndex;
    private float maxDistanceFromCenter;
    private float maxNodeVelocity;

    public void Initialize(SlimeCharacterController controller, Rigidbody2D center, int layer, float maxDistance, float maxVelocity)
    {
        slimeController = controller;
        centerBody = center;
        nodeRigidbody = GetComponent<Rigidbody2D>();
        layerIndex = layer;
        maxDistanceFromCenter = maxDistance;
        maxNodeVelocity = maxVelocity;
    }

    void Update()
    {
        // Periodically check if node is stuck
        if (Time.time - lastStuckCheck > 0.3f)
        {
            CheckIfStuck();
            lastStuckCheck = Time.time;
        }

        // Handle stuck timer
        if (isStuck)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 2f) // If stuck for more than 2 seconds
            {
                ForceUnstuck();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // Для внешнего слоя проверяем расстояние от центра
        if (layerIndex == 2) // surface layer
        {
            float distanceFromCenter = Vector3.Distance(transform.position, centerBody.transform.position);
            if (distanceFromCenter > maxDistanceFromCenter)
            {
                // Возвращаем узел ближе к центру
                Vector3 direction = (centerBody.transform.position - transform.position).normalized;
                nodeRigidbody.AddForce(direction * 50f);
            }
        }
    }

    void CheckIfStuck()
    {
        if (nodeRigidbody == null || centerBody == null) return;

        float distanceFromCenter = Vector2.Distance(centerBody.position, nodeRigidbody.position);
        float maxReasonableDistance = 3f; // Maximum reasonable distance from center

        // Node is considered stuck if:
        // 1. It's very far from center
        // 2. It's not moving much
        // 3. It's experiencing collision but not moving
        bool shouldBeStuck = distanceFromCenter > maxReasonableDistance &&
                            nodeRigidbody.linearVelocity.magnitude < 0.1f;

        if (shouldBeStuck && !isStuck)
        {
            isStuck = true;
            slimeController.ReportNodeStuck(nodeRigidbody);
        }
        else if (!shouldBeStuck && isStuck)
        {
            isStuck = false;
        }
    }

    void ForceUnstuck()
    {
        if (centerBody == null || nodeRigidbody == null) return;

        // Calculate direction back to center with some randomness
        Vector2 toCenter = (centerBody.position - nodeRigidbody.position).normalized;
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.5f;
        Vector2 unstuckDirection = (toCenter + randomDir).normalized;

        // Teleport node closer to center
        float teleportDistance = 1f;
        Vector2 newPosition = centerBody.position + unstuckDirection * teleportDistance;
        nodeRigidbody.MovePosition(newPosition);

        // Add force away from obstacles
        nodeRigidbody.AddForce(unstuckDirection * 200f, ForceMode2D.Impulse);

        isStuck = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject != null && collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            // Report ground contact only for surface nodes (layer 2)
            if (layerIndex == 2)
            {
                slimeController.ReportSurfaceGroundContact(true);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject != null && collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;

            // Report ground contact only for surface nodes (layer 2)
            if (layerIndex == 2)
            {
                slimeController.ReportSurfaceGroundContact(false);
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Если collision или его transform равен null, выходим
        if (collision == null || collision.transform == null || nodeRigidbody == null) return;

        // If node is in continuous collision and not moving much, it might be stuck
        if (nodeRigidbody.linearVelocity.magnitude < 0.2f &&
            collision.gameObject.CompareTag("Ground"))
        {
            // Add slight force to try to unstick
            Vector2 repelDir = (nodeRigidbody.position - (Vector2)collision.transform.position).normalized;
            if (repelDir.magnitude < 0.1f) repelDir = Random.insideUnitCircle.normalized;

            nodeRigidbody.AddForce(repelDir * 30f);
        }
    }
}