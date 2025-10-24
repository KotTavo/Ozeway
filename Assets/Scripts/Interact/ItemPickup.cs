// PickupInteractable.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField] private bool useGlobalScale = true;
    [SerializeField] private float customScaleReduction = 0.2f;
    [SerializeField] private string itemId = "Item";
    [SerializeField] private bool isStackable = false;
    [SerializeField] private int stackAmount = 1;

    [Header("Container Settings")]
    [SerializeField] private string containerTag = "Player";
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Floating Animation")]
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Transform container;
    private Vector3 originalScale;
    private bool isPickedUp = false;
    private Vector3 baseLocalPosition;
    private Collider2D itemCollider;
    private Rigidbody2D rb;

    // Public properties for other systems
    public string ItemId => itemId;
    public bool IsStackable => isStackable;
    public int StackAmount => stackAmount;
    public bool IsPickedUp => isPickedUp;

    private void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
    }

    public string GetInteractionText()
    {
        return $"Pick up {itemId}";
    }

    public void OnInteract(GameObject interactor)
    {
        if (isPickedUp) return;

        if (FindContainer(interactor))
        {
            StartCoroutine(PickupAnimation());
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        return !isPickedUp && FindContainer(interactor);
    }

    public void OnHighlightStart(GameObject interactor)
    {
        // Визуальная обратная связь при наведении
        transform.localScale = originalScale * 1.1f;
    }

    public void OnHighlightEnd(GameObject interactor)
    {
        transform.localScale = originalScale;
    }

    private bool FindContainer(GameObject interactor)
    {
        if (container != null) return true;

        GameObject containerObj = GameObject.FindGameObjectWithTag(containerTag);
        if (containerObj != null && containerObj.GetComponent<Collider2D>() != null)
        {
            container = containerObj.transform;
            return true;
        }

        return false;
    }

    private IEnumerator PickupAnimation()
    {
        isPickedUp = true;

        // Отключаем физику и коллизии
        if (itemCollider != null) itemCollider.enabled = false;
        if (rb != null) rb.simulated = false;

        // Вычисляем конечный масштаб
        float targetScale = useGlobalScale ?
            PickupManager.GlobalScaleReduction : customScaleReduction;
        Vector3 finalScale = originalScale * targetScale;

        // Анимация подбора
        float duration = 0.6f;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;

        // Вычисляем позицию внутри контейнера
        Vector3 containerLocalPos = GetPositionInsideContainer();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = floatCurve.Evaluate(t);

            // Плавное перемещение к контейнеру
            transform.position = Vector3.Lerp(startPosition,
                container.position + containerLocalPos, curveT);

            // Плавное масштабирование
            transform.localScale = Vector3.Lerp(startScale, finalScale, curveT);

            yield return null;
        }

        // Делаем дочерним объектом контейнера
        transform.SetParent(container);
        transform.localPosition = containerLocalPos;
        baseLocalPosition = containerLocalPos;

        // Запускаем анимацию плавания
        StartCoroutine(FloatingAnimation());

        // Уведомляем систему инвентаря
        InventorySystem.Instance?.AddItem(this);
    }

    private IEnumerator FloatingAnimation()
    {
        float time = 0f;

        while (isPickedUp && container != null)
        {
            time += Time.deltaTime;

            // Вертикальное плавающее движение
            float yOffset = Mathf.Sin(time * floatSpeed) * floatHeight;
            Vector3 newPosition = baseLocalPosition + new Vector3(0, yOffset, 0);
            transform.localPosition = newPosition;

            // Медленное вращение
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            yield return null;
        }
    }

    private Vector3 GetPositionInsideContainer()
    {
        Collider2D containerCollider = container.GetComponent<Collider2D>();
        if (containerCollider == null) return localOffset;

        // Генерируем случайную позицию внутри коллайдера контейнера
        Vector2 randomPoint = GetRandomPointInCollider(containerCollider);
        return container.InverseTransformPoint(randomPoint) + localOffset;
    }

    private Vector2 GetRandomPointInCollider(Collider2D collider)
    {
        if (collider is CircleCollider2D circleCollider)
        {
            // Для круглого коллайдера
            Vector2 center = circleCollider.bounds.center;
            float radius = circleCollider.radius * Mathf.Max(
                circleCollider.transform.lossyScale.x,
                circleCollider.transform.lossyScale.y);

            return center + Random.insideUnitCircle * radius;
        }
        else if (collider is BoxCollider2D boxCollider)
        {
            // Для прямоугольного коллайдера
            Bounds bounds = boxCollider.bounds;
            return new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
        }
        else
        {
            // Для других типов коллайдеров используем bounds
            Bounds bounds = collider.bounds;
            return new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
        }
    }

    // Метод для сброса предмета (для будущей механики выбрасывания)
    public void DropItem(Vector3 position)
    {
        if (!isPickedUp) return;

        StopAllCoroutines();
        transform.SetParent(null);
        transform.position = position;
        transform.localScale = originalScale;

        if (itemCollider != null) itemCollider.enabled = true;
        if (rb != null) rb.simulated = true;

        isPickedUp = false;
    }
}