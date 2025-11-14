using UnityEngine;

public abstract class CoreAbility : ScriptableObject
{
    [Header("Базовая информация")]
    public string abilityName;
    public string description;
    public float cooldown = 1f;
    public Sprite icon;

    [Header("Визуальные эффекты")]
    public GameObject castEffect;
    public AudioClip castSound;

    protected SlimeCharacterController slimeController;
    protected CoreInventory coreInventory;
    protected Camera mainCamera;

    protected float lastUsedTime;
    protected bool isOnCooldown = false;

    public virtual void Initialize(SlimeCharacterController slime, CoreInventory inventory)
    {
        slimeController = slime;
        coreInventory = inventory;
        mainCamera = Camera.main;
        lastUsedTime = -cooldown; // Чтобы способность была доступна сразу
    }

    public abstract void ExecuteAbility();

    public virtual bool CanExecute()
    {
        if (slimeController == null)
        {
            Debug.LogWarning($"Ability {abilityName} cannot execute: slimeController is null");
            return false;
        }

        if (isOnCooldown)
        {
            float timeSinceLastUse = Time.time - lastUsedTime;
            if (timeSinceLastUse < cooldown)
            {
                Debug.Log($"Ability {abilityName} on cooldown: {cooldown - timeSinceLastUse:F1}s remaining");
                return false;
            }
            isOnCooldown = false;
        }

        return true;
    }

    protected void StartCooldown()
    {
        lastUsedTime = Time.time;
        isOnCooldown = true;
    }

    protected Vector2 GetMouseDirection()
    {
        if (mainCamera == null || slimeController == null)
            return Vector2.right;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - slimeController.transform.position).normalized;
        return direction;
    }

    protected Vector2 GetShootSpawnPosition(Vector2 direction, float offset = 0.5f)
    {
        if (slimeController == null)
            return Vector2.zero;

        // Получаем коллайдер слизи для определения границы
        CircleCollider2D collider = slimeController.GetComponent<CircleCollider2D>();
        float radius = collider != null ? collider.radius : 0.5f;

        return (Vector2)slimeController.transform.position + direction * (radius + offset);
    }

    protected void PlayCastEffects(Vector3 position)
    {
        // Визуальный эффект
        if (castEffect != null)
        {
            GameObject effect = Instantiate(castEffect, position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Звуковой эффект
        if (castSound != null && slimeController != null)
        {
            AudioSource.PlayClipAtPoint(castSound, position);
        }
    }
}