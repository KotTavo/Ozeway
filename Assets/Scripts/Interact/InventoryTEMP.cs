// InventorySystem.cs
using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int maxUniqueItems = 20;

    private Dictionary<string, int> stackedItems = new Dictionary<string, int>();
    private List<PickupInteractable> uniqueItems = new List<PickupInteractable>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Раскомментируйте если нужно сохранять между сценами:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(PickupInteractable item)
    {
        if (item.IsStackable)
        {
            AddStackableItem(item);
        }
        else
        {
            AddUniqueItem(item);
        }

        Debug.Log($"Item added: {item.ItemId}");
        // Здесь можно вызвать событие для обновления UI
    }

    private void AddStackableItem(PickupInteractable item)
    {
        if (stackedItems.ContainsKey(item.ItemId))
        {
            stackedItems[item.ItemId] += item.StackAmount;
        }
        else
        {
            stackedItems[item.ItemId] = item.StackAmount;
        }

        // Уничтожаем физический объект для стакируемых предметов
        Destroy(item.gameObject);
    }

    private void AddUniqueItem(PickupInteractable item)
    {
        if (uniqueItems.Count >= maxUniqueItems)
        {
            Debug.LogWarning("Inventory full! Cannot add unique item.");
            return;
        }

        uniqueItems.Add(item);
        // Не уничтожаем - предмет остается видимым в контейнере
    }

    public bool HasItem(string itemId)
    {
        return stackedItems.ContainsKey(itemId) && stackedItems[itemId] > 0 ||
               uniqueItems.Exists(item => item.ItemId == itemId);
    }

    public int GetItemCount(string itemId)
    {
        if (stackedItems.ContainsKey(itemId))
            return stackedItems[itemId];

        return uniqueItems.FindAll(item => item.ItemId == itemId).Count;
    }

    public void RemoveItem(string itemId, int amount = 1)
    {
        if (stackedItems.ContainsKey(itemId))
        {
            stackedItems[itemId] = Mathf.Max(0, stackedItems[itemId] - amount);
            if (stackedItems[itemId] == 0)
                stackedItems.Remove(itemId);
        }
    }
}