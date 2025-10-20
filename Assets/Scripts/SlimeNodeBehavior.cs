using UnityEngine;

public class SlimeNodeBehavior : MonoBehaviour
{
    [Header("=== ПОВЕДЕНИЕ СКОЛЬЖЕНИЯ ===")]
    public float personalSlideForce = 15f;
    public float slideDetectionDistance = 0.5f;

    public int layerIndex { get; private set; }

    private SlimeCharacterController slimeController;
    private SlimeNavigationSystem navigationSystem;
    private Rigidbody2D nodeRigidbody;
    private Rigidbody2D centerBody;
    private float lastStuckCheck;
    private bool isStuck = false;
    private float stuckTimer = 0f;
    private bool isGrounded = false;
    private float maxDistanceFromCenter;
    private float maxNodeVelocity;
    private Vector2 lastPosition;
    private float stuckPositionTimer = 0f;
    private float lastSlideCheck;
    private const float SLIDE_CHECK_INTERVAL = 0.2f;

    // НОВОЕ: для лучшего отслеживания застревания
    private float averageVelocity = 0f;
    private const float VELOCITY_SMOOTHING = 0.9f;

    public void Initialize(SlimeCharacterController controller, Rigidbody2D center, int layer, float maxDistance, float maxVelocity)
    {
        slimeController = controller;
        centerBody = center;
        nodeRigidbody = GetComponent<Rigidbody2D>();
        layerIndex = layer;
        maxDistanceFromCenter = maxDistance;
        maxNodeVelocity = maxVelocity;
        lastPosition = nodeRigidbody.position;

        navigationSystem = controller.GetComponent<SlimeNavigationSystem>();
    }

    void Update()
    {
        if (Time.time - lastStuckCheck > 0.3f)
        {
            CheckIfStuck();
            lastStuckCheck = Time.time;
        }

        // УЛУЧШЕННОЕ отслеживание застревания
        float currentSpeed = nodeRigidbody.linearVelocity.magnitude;
        averageVelocity = averageVelocity * VELOCITY_SMOOTHING + currentSpeed * (1f - VELOCITY_SMOOTHING);

        if (Vector2.Distance(nodeRigidbody.position, lastPosition) < 0.02f)
        {
            stuckPositionTimer += Time.deltaTime;
        }
        else
        {
            stuckPositionTimer = Mathf.Max(0, stuckPositionTimer - Time.deltaTime * 2f);
            lastPosition = nodeRigidbody.position;
        }

        if (Time.time - lastSlideCheck > SLIDE_CHECK_INTERVAL)
        {
            CheckPersonalSliding();
            lastSlideCheck = Time.time;
        }
    }

    void CheckIfStuck()
    {
        if (nodeRigidbody == null || centerBody == null) return;

        float distanceFromCenter = Vector2.Distance(centerBody.position, nodeRigidbody.position);

        bool isFarFromCenter = distanceFromCenter > maxDistanceFromCenter * 0.8f;
        bool isNotMoving = averageVelocity < 0.3f; // Используем сглаженную скорость
        bool centerIsMoving = slimeController.GetCenterVelocity().magnitude > 0.5f;
        bool isPositionStuck = stuckPositionTimer > 1.0f; // Уменьшил время для более быстрой реакции

        bool shouldBeStuck = (isFarFromCenter && isNotMoving && centerIsMoving) || isPositionStuck;

        if (shouldBeStuck && !isStuck)
        {
            isStuck = true;
            stuckTimer = 0f;
            slimeController.ReportNodeStuck(nodeRigidbody);

            if (navigationSystem != null)
            {
                navigationSystem.ForceNavigateNode(nodeRigidbody);
            }
        }
        else if (!shouldBeStuck && isStuck)
        {
            isStuck = false;
            stuckPositionTimer = 0f;
        }

        if (isStuck)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 2f) // Уменьшил время до принудительного освобождения
            {
                ForceUnstuck();
                stuckTimer = 0f;
            }
        }
    }

    void CheckPersonalSliding()
    {
        if (nodeRigidbody == null) return;

        if (IsOnSlidableSurface() && slimeController.GetCenterVelocity().magnitude > 0.3f)
        {
            ApplyPersonalSlideForce();
        }
    }

    private bool IsOnSlidableSurface()
    {
        RaycastHit2D hit = Physics2D.Raycast(nodeRigidbody.position, Vector2.down, slideDetectionDistance, slimeController.obstacleLayers);
        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            float surfaceAngle = Vector2.Angle(hit.normal, Vector2.up);
            return surfaceAngle < 60f;
        }
        return false;
    }

    private void ApplyPersonalSlideForce()
    {
        Vector2 slideDirection = slimeController.GetCenterVelocity().normalized;
        nodeRigidbody.AddForce(slideDirection * personalSlideForce);
        Debug.DrawRay(nodeRigidbody.position, slideDirection * 0.3f, Color.cyan);
    }

    void ForceUnstuck()
    {
        if (centerBody == null || nodeRigidbody == null) return;

        Vector2 toCenter = (centerBody.position - nodeRigidbody.position).normalized;
        Vector2 randomDir = Random.insideUnitCircle.normalized * 0.2f; // Уменьшил случайное смещение
        Vector2 unstuckDirection = (toCenter + randomDir).normalized;

        // Более мягкое освобождение
        nodeRigidbody.AddForce(unstuckDirection * 150f, ForceMode2D.Impulse);

        isStuck = false;
        stuckTimer = 0f;
        stuckPositionTimer = 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            if (layerIndex == 2)
            {
                slimeController.ReportSurfaceGroundContact(true);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;

            if (layerIndex == 2)
            {
                slimeController.ReportSurfaceGroundContact(false);
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                float contactAngle = Vector2.Angle(contact.normal, Vector2.up);

                if (contactAngle < 45f)
                {
                    Vector2 slideDir = new Vector2(-contact.normal.y, contact.normal.x);
                    if (Vector2.Dot(slideDir, slimeController.GetCenterVelocity().normalized) < 0)
                        slideDir = -slideDir;

                    nodeRigidbody.AddForce(slideDir * personalSlideForce * 0.5f);
                }
            }

            // УСИЛЕННОЕ отталкивание от препятствий
            if (nodeRigidbody.linearVelocity.magnitude < 0.3f)
            {
                ContactPoint2D contact = collision.contacts[0];
                Vector2 pushDir = (nodeRigidbody.position - contact.point).normalized;
                nodeRigidbody.AddForce(pushDir * 15f); // Увеличил силу отталкивания
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (nodeRigidbody == null) return;

        if (isStuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
        else if (isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.08f);
        }
        else
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}