using UnityEngine;

[CreateAssetMenu(fileName = "Ability_AcidBulletShot", menuName = "Slime Cores/Abilities/AcidBulletShot")]
public class AcidBulletShot : CoreAbility
{
    [Header("Параметры стрельбы")]
    public float bulletSpeed = 15f;
    public float bulletLifetime = 3f;
    public float bulletDamage = 10f;
    public float spawnOffset = 0.3f;

    [Header("Префабы и эффекты")]
    public GameObject acidBulletPrefab;
    public LayerMask collisionLayers = ~0;

    public override void ExecuteAbility()
    {
        if (!CanExecute() || acidBulletPrefab == null)
        {
            Debug.LogError("AcidBulletShot cannot execute: " +
                         (acidBulletPrefab == null ? "Prefab not assigned" : "Cooldown or controller issue"));
            return;
        }

        // Получаем направление к мыши
        Vector2 shootDirection = GetMouseDirection();
        if (shootDirection == Vector2.zero)
        {
            shootDirection = Vector2.right; // Направление по умолчанию
        }

        // Получаем позицию спауна на краю коллайдера слизи
        Vector2 spawnPosition = GetShootSpawnPosition(shootDirection, spawnOffset);

        // Создаем пулю
        CreateBullet(spawnPosition, shootDirection);

        // Эффекты каста
        PlayCastEffects(spawnPosition);

        // Запускаем кулдаун
        StartCooldown();

        Debug.Log($"Acid bullet shot towards: {shootDirection} from position: {spawnPosition}");
    }

    private void CreateBullet(Vector2 spawnPosition, Vector2 direction)
    {
        GameObject bullet = Instantiate(acidBulletPrefab, spawnPosition, Quaternion.identity);

        // Направляем пулю в сторону мыши
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Инициализируем контроллер пули
        AcidBulletController bulletController = bullet.GetComponent<AcidBulletController>();
        if (bulletController != null)
        {
            bulletController.Initialize(direction, bulletSpeed, bulletLifetime, bulletDamage, collisionLayers);
        }
        else
        {
            Debug.LogError("AcidBulletController component is missing on the prefab!");
            // Добавляем контроллер автоматически если забыли
            bulletController = bullet.AddComponent<AcidBulletController>();
            bulletController.Initialize(direction, bulletSpeed, bulletLifetime, bulletDamage, collisionLayers);
        }
    }

    public override bool CanExecute()
    {
        return base.CanExecute() && acidBulletPrefab != null;
    }
}