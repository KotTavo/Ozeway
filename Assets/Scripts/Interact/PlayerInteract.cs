// PlayerInteractor.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private float checkRate = 0.1f;
    [SerializeField] private LayerMask interactableLayers = -1;

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Text interactionTextUI;

    private Interactable currentInteractable;
    private float lastCheckTime;
    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        // Проверяем взаимодействия с заданной частотой
        if (Time.time - lastCheckTime >= checkRate)
        {
            FindNearestInteractable();
            lastCheckTime = Time.time;
        }

        // Обработка ввода
        if (Input.GetKeyDown(interactionKey) && currentInteractable != null)
        {
            if (currentInteractable.CanInteract(gameObject))
            {
                currentInteractable.Interact(gameObject);
            }
        }

        UpdateUI();
    }

    private void FindNearestInteractable()
    {
        Interactable nearestInteractable = null;
        float nearestDistance = float.MaxValue;

        // Находим все Interactable объекты в сцене
        var allInteractables = FindObjectsOfType<Interactable>();

        foreach (var interactable in allInteractables)
        {
            if (interactable.CanInteract(gameObject))
            {
                float distance = Vector3.Distance(transform.position, interactable.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                }
            }
        }

        // Обновляем текущий interactable
        if (currentInteractable != nearestInteractable)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Unhighlight(gameObject);
            }

            currentInteractable = nearestInteractable;

            if (currentInteractable != null)
            {
                currentInteractable.Highlight(gameObject);
            }
        }
    }

    private void UpdateUI()
    {
        if (interactionTextUI == null) return;

        if (currentInteractable != null && currentInteractable.CanInteract(gameObject))
        {
            interactionTextUI.text = $"[{interactionKey}] {currentInteractable.GetInteractionText(gameObject)}";
            interactionTextUI.gameObject.SetActive(true);
        }
        else
        {
            interactionTextUI.gameObject.SetActive(false);
        }
    }

    // Для отладки
    private void OnDrawGizmos()
    {
        if (currentInteractable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentInteractable.transform.position);
        }
    }
}