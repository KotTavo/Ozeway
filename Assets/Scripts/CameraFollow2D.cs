using UnityEngine;

public class SmoothCameraFollow2D : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // Объект, за которым следует камера
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f); // Смещение камеры

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeedInsideZone = 0.1f; // Плавность внутри Safe зоны
    [SerializeField] private float smoothSpeedOutsideZone = 0.3f; // Плавность вне Safe зоны
    [SerializeField] private float maxSpeed = 10f; // Максимальная скорость движения камеры
    [SerializeField] private float zoomSpeed = 2f; // Скорость изменения зума

    [Header("Safe Zone Settings")]
    [SerializeField] private Vector2 safeZoneSize = new Vector2(2f, 1f); // Размер Safe зоны
    [SerializeField] private bool showSafeZoneGizmo = true; // Показывать зону в редакторе
    [SerializeField] private Color safeZoneColor = new Color(0.5f, 0.8f, 1f, 0.2f); // Цвет зоны

    [Header("Bounds Settings")]
    [SerializeField] private bool useBounds = false; // Ограничивать движение камеры
    [SerializeField] private Vector2 minBounds; // Минимальные границы
    [SerializeField] private Vector2 maxBounds; // Максимальные границы

    private Vector3 velocity = Vector3.zero;
    private Bounds safeZoneBounds;
    private float targetOrthographicSize;
    private bool isFocusMode = false;
    private Vector3 focusPosition;
    private float focusSize;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Camera target is not assigned!");
            enabled = false;
            return;
        }

        targetOrthographicSize = Camera.main.orthographicSize;
        UpdateSafeZone();
    }

    private void LateUpdate()
    {
        if (isFocusMode)
        {
            HandleFocusMode();
        }
        else
        {
            HandleFollowMode();
        }

        UpdateCameraZoom();
    }

    private void HandleFollowMode()
    {
        UpdateSafeZone();

        // Вычисляем желаемую позицию камеры
        Vector3 targetPosition = target.position + offset;

        // Определяем, находится ли цель вне Safe зоны
        bool isOutsideSafeZone = !safeZoneBounds.Contains(target.position);

        // Выбираем скорость в зависимости от положения цели
        float currentSmoothSpeed = isOutsideSafeZone ? smoothSpeedOutsideZone : smoothSpeedInsideZone;

        // Если включены границы, ограничиваем позицию камеры
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Плавное перемещение камеры с ограничением скорости
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            currentSmoothSpeed,
            maxSpeed,
            Time.deltaTime
        );
    }

    private void HandleFocusMode()
    {
        // Плавное перемещение камеры к точке фокусировки
        transform.position = Vector3.SmoothDamp(
            transform.position,
            focusPosition,
            ref velocity,
            smoothSpeedInsideZone,
            maxSpeed,
            Time.deltaTime
        );
    }

    private void UpdateCameraZoom()
    {
        // Плавное изменение размера камеры
        Camera.main.orthographicSize = Mathf.Lerp(
            Camera.main.orthographicSize,
            targetOrthographicSize,
            zoomSpeed * Time.deltaTime);
    }

    private void UpdateSafeZone()
    {
        // Обновляем Safe зону (центр вокруг текущей позиции камеры)
        safeZoneBounds = new Bounds(transform.position, safeZoneSize);
    }

    // Метод для активации режима фокусировки
    public void EnterFocusMode(Vector3 position, float zoomSize)
    {
        isFocusMode = true;
        focusPosition = new Vector3(position.x, position.y, transform.position.z);
        targetOrthographicSize = zoomSize;
    }

    // Метод для выхода из режима фокусировки
    public void ExitFocusMode()
    {
        isFocusMode = false;
        targetOrthographicSize = GetComponent<Camera>().orthographicSize;
    }

    // Метод для установки границ камеры
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    // Метод для изменения размера Safe зоны
    public void SetSafeZoneSize(Vector2 newSize)
    {
        safeZoneSize = newSize;
        UpdateSafeZone();
    }

    // Отрисовка Safe зоны в редакторе
    private void OnDrawGizmos()
    {
        if (!showSafeZoneGizmo || !Application.isPlaying) return;

        Gizmos.color = safeZoneColor;
        Gizmos.DrawCube(safeZoneBounds.center, safeZoneBounds.size);
    }
}