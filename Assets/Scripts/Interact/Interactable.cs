// Interactable.cs
using UnityEngine;
using System.Collections.Generic;

public class Interactable : MonoBehaviour
{
    [Header("Base Interactable Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    [Header("Interaction Settings")]
    [SerializeField] private string interactionText = "Interact";
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers = 1;

    // События для кастомной логики
    public System.Action<GameObject> OnInteractEvent;
    public System.Action<GameObject> OnHighlightEvent;
    public System.Action<GameObject> OnUnhighlightEvent;

    private List<IInteractable> interactableModules = new List<IInteractable>();
    private bool isHighlighted = false;

    public float InteractionRadius => interactionRadius;
    public string InteractionText => interactionText;

    private void Awake()
    {
        // Автоматически находим все модули IInteractable на этом GameObject
        interactableModules.AddRange(GetComponents<IInteractable>());
    }

    public void Interact(GameObject interactor)
    {
        // Вызываем базовые события
        OnInteractEvent?.Invoke(interactor);

        // Вызываем все модули IInteractable
        foreach (var module in interactableModules)
        {
            if (module.CanInteract(interactor))
            {
                module.OnInteract(interactor);
            }
        }
    }

    public void Highlight(GameObject interactor)
    {
        if (isHighlighted) return;

        isHighlighted = true;
        OnHighlightEvent?.Invoke(interactor);

        foreach (var module in interactableModules)
        {
            module.OnHighlightStart(interactor);
        }
    }

    public void Unhighlight(GameObject interactor)
    {
        if (!isHighlighted) return;

        isHighlighted = false;
        OnUnhighlightEvent?.Invoke(interactor);

        foreach (var module in interactableModules)
        {
            module.OnHighlightEnd(interactor);
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        // Проверка расстояния
        float distance = Vector3.Distance(transform.position, interactor.transform.position);
        if (distance > interactionRadius) return false;

        // Проверка линии видимости
        if (requireLineOfSight)
        {
            Vector3 direction = (transform.position - interactor.transform.position).normalized;
            if (Physics.Raycast(interactor.transform.position, direction, out RaycastHit hit, interactionRadius, obstacleLayers))
            {
                if (hit.collider.gameObject != gameObject) return false;
            }
        }

        // Проверка модулей
        foreach (var module in interactableModules)
        {
            if (!module.CanInteract(interactor)) return false;
        }

        return true;
    }

    public string GetInteractionText(GameObject interactor)
    {
        // Если есть модули, используем текст первого доступного модуля
        foreach (var module in interactableModules)
        {
            if (module.CanInteract(interactor))
            {
                return module.GetInteractionText();
            }
        }

        return interactionText;
    }

    // Визуализация радиуса в редакторе
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        // Рисуем иконку для лучшей видимости
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    // Добавление модулей во время выполнения
    public void RegisterInteractableModule(IInteractable module)
    {
        if (!interactableModules.Contains(module))
        {
            interactableModules.Add(module);
        }
    }

    public void UnregisterInteractableModule(IInteractable module)
    {
        interactableModules.Remove(module);
    }
}