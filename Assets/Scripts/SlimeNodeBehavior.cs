using UnityEngine;

public class SlimeNodeBehavior : MonoBehaviour
{
    private SlimeCharacterController slimeController;
    private Rigidbody2D nodeRigidbody;
    private Rigidbody2D centerBody;
    private float lastStuckCheck;
    private bool isStuck = false;
    private float stuckTimer = 0f;

    public void Initialize(SlimeCharacterController controller, Rigidbody2D center)
    {
        slimeController = controller;
        centerBody = center;
        nodeRigidbody = GetComponent<Rigidbody2D>();
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
        if (centerBody == null) return;

        // Calculate direction to center with some randomness
        Vector2 toCenter = (centerBody.position - nodeRigidbody.position).normalized;
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.5f;
        Vector2 unstuckDir = (toCenter + randomDir).normalized;

        // Teleport node closer to center
        float teleportDistance = 1f;
        Vector2 newPosition = centerBody.position + unstuckDir * teleportDistance;
        nodeRigidbody.MovePosition(newPosition);

        // Add force away from obstacles
        nodeRigidbody.AddForce(unstuckDir * 200f, ForceMode2D.Impulse);

        isStuck = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // If node is in continuous collision and not moving much, it might be stuck
        if (nodeRigidbody.linearVelocity.magnitude < 0.2f &&
            (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Obstacle")))
        {
            // Add slight force to try to unstick
            Vector2 repelDir = (nodeRigidbody.position - (Vector2)collision.transform.position).normalized;
            if (repelDir.magnitude < 0.1f) repelDir = Random.insideUnitCircle.normalized;

            nodeRigidbody.AddForce(repelDir * 30f);
        }
    }
}