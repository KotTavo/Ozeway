using UnityEngine;

public class AcidBulletController : MonoBehaviour
{
    [Header("Bullet Components")]
    public GameObject hitEffect;
    public AudioClip hitSound;

    private Vector2 direction;
    private float speed;
    private float lifetime;
    private float damage;
    private LayerMask collisionMask;

    private Rigidbody2D rb;
    private bool hasHit = false;
    private float creationTime;

    public void Initialize(Vector2 moveDirection, float moveSpeed, float bulletLifetime, float bulletDamage, LayerMask collisionLayers)
    {
        direction = moveDirection.normalized;
        speed = moveSpeed;
        lifetime = bulletLifetime;
        damage = bulletDamage;
        collisionMask = collisionLayers;
        creationTime = Time.time;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Уничтожаем через время
        Destroy(gameObject, lifetime);

        // Начальная скорость
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void Update()
    {
        // Автоматическое уничтожение если пуля существует слишком долго
        if (Time.time - creationTime > lifetime && !hasHit)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && !hasHit)
        {
            // Поддерживаем постоянную скорость
            rb.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        // Игнорируем триггеры и самого игрока
        if (collision.isTrigger || collision.CompareTag("Player"))
            return;

        // Проверяем слой столкновения
        if (((1 << collision.gameObject.layer) & collisionMask) != 0)
        {
            hasHit = true;

            // Наносим урон если есть здоровье
            Health health = collision.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log($"Acid bullet hit {collision.name} for {damage} damage");
            }

            // Визуальные и звуковые эффекты при попадании
            CreateHitEffect();

            // Уничтожаем пулю
            Destroy(gameObject);
        }
    }

    private void CreateHitEffect()
    {
        // Визуальный эффект
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Звуковой эффект
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    // Визуализация в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 0.5f);
    }
}