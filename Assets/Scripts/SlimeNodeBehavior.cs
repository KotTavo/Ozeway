using UnityEngine;

public class SlimeNodeBehavior : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ СКОЛЬЖЕНИЯ ===")]
    public float personalSlideForce = 15f;
    public float slideDetectionDistance = 0.5f;

    public int layerIndex { get; private set; }

    private SlimeCharacterController slimeController;
    private Rigidbody2D nodeRigidbody;
    private Rigidbody2D centerBody;
    private Collider2D nodeCollider;
    private float lastStuckCheck;
    private bool isStuck = false;
    private float stuckTimer = 0f;
    private bool isGrounded = false;
    private float maxDistanceFromCenter;
    private float maxNodeVelocity;
    private float unstuckRadius;
    private float autoGatherRadius;
    private float inputStuckRadius;

    // Системы восстановления
    private bool isRecovering = false;
    private bool isAutoGathering = false;
    private float recoveryTimer = 0f;
    private const float RECOVERY_DURATION = 1.0f;
    private const float AUTO_GATHER_DURATION = 0.7f;

    // Флаг завершенной инициализации
    private bool isInitialized = false;

    public void Initialize(SlimeCharacterController controller, Rigidbody2D center, int layer,
                          float maxDistance, float maxVelocity,
                          float unstuckRadiusValue, float autoGatherRadiusValue, float inputStuckRadiusValue)
    {
        slimeController = controller;
        centerBody = center;
        nodeRigidbody = GetComponent<Rigidbody2D>();
        nodeCollider = GetComponent<Collider2D>();
        layerIndex = layer;
        maxDistanceFromCenter = maxDistance;
        maxNodeVelocity = maxVelocity;
        unstuckRadius = unstuckRadiusValue;
        autoGatherRadius = autoGatherRadiusValue;
        inputStuckRadius = inputStuckRadiusValue;

        // Проверяем критически важные компоненты
        if (nodeRigidbody == null)
        {
            Debug.LogError($"SlimeNodeBehavior: Rigidbody2D not found on {gameObject.name}");
            return;
        }

        if (slimeController == null)
        {
            Debug.LogError($"SlimeNodeBehavior: SlimeController not assigned on {gameObject.name}");
            return;
        }

        if (centerBody == null)
        {
            Debug.LogError($"SlimeNodeBehavior: CenterBody not assigned on {gameObject.name}");
            return;
        }

        isInitialized = true;
    }

    // Метод для безопасной проверки инициализации
    private bool IsProperlyInitialized()
    {
        return isInitialized &&
               nodeRigidbody != null &&
               slimeController != null &&
               centerBody != null;
    }

    void Update()
    {
        if (!IsProperlyInitialized()) return;

        // Проверка застревания каждые 0.3 секунды
        if (Time.time - lastStuckCheck > 0.3f)
        {
            CheckIfStuck();
            lastStuckCheck = Time.time;
        }

        // Обработка восстановления
        if (isRecovering)
        {
            HandleRecovery();
        }

        // Обработка автоматического кучкования
        if (isAutoGathering)
        {
            HandleAutoGather();
        }
    }

    void CheckIfStuck()
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        float distanceFromCenter = Vector2.Distance(centerBody.position, nodeRigidbody.position);

        // Основное застревание - за пределами максимального радиуса
        bool shouldBeStuck = distanceFromCenter > unstuckRadius &&
                            nodeRigidbody.linearVelocity.magnitude < 0.2f;

        if (shouldBeStuck && !isStuck)
        {
            StartStuckRecovery();
        }
        else if (!shouldBeStuck && isStuck)
        {
            isStuck = false;
            stuckTimer = 0f;
        }

        // Аварийное восстановление после длительного застревания
        if (isStuck)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3f)
            {
                StartStuckRecovery();
                stuckTimer = 0f;
            }
        }
    }

    public void StartAutoGather()
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        isAutoGathering = true;
        recoveryTimer = 0f;

        // Отключаем коллизию на время кучкования
        if (nodeCollider != null)
        {
            nodeCollider.enabled = false;
        }
    }

    public void ForceGather()
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        float distanceFromCenter = Vector2.Distance(centerBody.position, nodeRigidbody.position);
        if (distanceFromCenter > inputStuckRadius)
        {
            StartAutoGather();
        }
    }

    void HandleAutoGather()
    {
        if (!IsProperlyInitialized()) return;

        recoveryTimer += Time.deltaTime;

        if (recoveryTimer < AUTO_GATHER_DURATION)
        {
            // Плавное перемещение не к самому центру, а к границе autoGatherRadius
            float targetDistance = autoGatherRadius * 0.6f;
            Vector2 targetDirection = (centerBody.position - nodeRigidbody.position).normalized;
            Vector2 targetPosition = (Vector2)centerBody.position - targetDirection * targetDistance;

            Vector2 newPosition = Vector2.Lerp(nodeRigidbody.position, targetPosition, recoveryTimer / AUTO_GATHER_DURATION);

            // Добавляем небольшое случайное смещение для естественности
            if (recoveryTimer > AUTO_GATHER_DURATION * 0.5f)
            {
                newPosition += Random.insideUnitCircle * 0.1f;
            }

            nodeRigidbody.MovePosition(newPosition);
        }
        else
        {
            // Завершение автоматического кучкования
            EndAutoGather();
        }
    }

    void EndAutoGather()
    {
        if (!IsProperlyInitialized()) return;

        isAutoGathering = false;
        recoveryTimer = 0f;

        // Включаем коллизию обратно
        if (nodeCollider != null)
        {
            nodeCollider.enabled = true;
        }

        // Даем небольшой импульс в случайном направлении
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        nodeRigidbody.AddForce(randomDir * 8f, ForceMode2D.Impulse);
    }

    void StartStuckRecovery()
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        isStuck = true;
        isRecovering = true;
        recoveryTimer = 0f;

        // Отключаем коллизию на время восстановления
        if (nodeCollider != null)
        {
            nodeCollider.enabled = false;
        }
    }

    void HandleRecovery()
    {
        if (!IsProperlyInitialized()) return;

        recoveryTimer += Time.deltaTime;

        if (recoveryTimer < RECOVERY_DURATION)
        {
            // Плавное перемещение к центру с небольшим случайным смещением
            Vector2 targetPosition = Vector2.Lerp(nodeRigidbody.position, centerBody.position, recoveryTimer / RECOVERY_DURATION);

            // Добавляем небольшое случайное смещение
            if (recoveryTimer > RECOVERY_DURATION * 0.7f)
            {
                targetPosition += Random.insideUnitCircle * 0.15f;
            }

            nodeRigidbody.MovePosition(targetPosition);
        }
        else
        {
            // Завершение восстановления
            EndRecovery();
        }
    }

    void EndRecovery()
    {
        if (!IsProperlyInitialized()) return;

        isRecovering = false;
        isStuck = false;
        stuckTimer = 0f;

        // Включаем коллизию обратно
        if (nodeCollider != null)
        {
            nodeCollider.enabled = true;
        }

        // Даем небольшой импульс в случайном направлении
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        nodeRigidbody.AddForce(randomDir * 12f, ForceMode2D.Impulse);
    }

    void FixedUpdate()
    {
        if (!IsProperlyInitialized()) return;

        // Применяем силы скольжения для улучшения движения по наклонным поверхностям
        if (!isRecovering && !isAutoGathering && IsOnSlidableSurface() && slimeController.GetCenterVelocity().magnitude > 0.3f)
        {
            ApplyPersonalSlideForce();
        }
    }

    bool IsOnSlidableSurface()
    {
        if (!IsProperlyInitialized()) return false;
        if (nodeCollider != null && !nodeCollider.enabled) return false;

        RaycastHit2D hit = Physics2D.Raycast(nodeRigidbody.position, Vector2.down, slideDetectionDistance, slimeController.obstacleLayers);
        return hit.collider != null && hit.collider.CompareTag("Ground");
    }

    void ApplyPersonalSlideForce()
    {
        if (!IsProperlyInitialized()) return;

        Vector2 slideDirection = slimeController.GetCenterVelocity().normalized;
        nodeRigidbody.AddForce(slideDirection * personalSlideForce);
    }

    public bool IsGathering()
    {
        return isRecovering || isAutoGathering;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            if (layerIndex == 2) // surface layer
            {
                slimeController.ReportSurfaceGroundContact(true);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;

            if (layerIndex == 2) // surface layer
            {
                slimeController.ReportSurfaceGroundContact(false);
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsProperlyInitialized() || isRecovering || isAutoGathering) return;

        if (collision.gameObject.CompareTag("Ground") && nodeRigidbody.linearVelocity.magnitude < 0.2f)
        {
            // Легкое подталкивание для предотвращения залипания
            ContactPoint2D contact = collision.contacts[0];
            Vector2 pushDir = (nodeRigidbody.position - contact.point).normalized;
            nodeRigidbody.AddForce(pushDir * 5f);
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (!IsProperlyInitialized()) return;

        if (isRecovering)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawLine(transform.position, centerBody.position);
        }
        else if (isAutoGathering)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
        else if (isStuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}