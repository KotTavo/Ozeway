using System.Collections.Generic;
using UnityEngine;

public class CoreInventory : MonoBehaviour
{
    [Header("Настройки инвентаря")]
    public int maxCores = 9;
    public int quickSlotCount = 3;

    [Header("Текущее состояние")]
    public List<CoreData> allCores = new List<CoreData>();
    public CoreData[] quickSlots;
    public CoreData activeCore;
    public int activeQuickSlotIndex = 0;

    private SlimeCharacterController slimeController;
    private CoreUIManager uiManager;
    private CoreOrbitController orbitController;

    // События для UI
    public System.Action<CoreData> OnCoreChanged;
    public System.Action OnQuickSlotsUpdated;

    void Start()
    {
        slimeController = GetComponent<SlimeCharacterController>();
        uiManager = FindObjectOfType<CoreUIManager>();
        orbitController = GetComponent<CoreOrbitController>();

        // Подписываемся на событие смены ядра
        OnCoreChanged += OnActiveCoreChanged;

        InitializeQuickSlots();

        if (allCores.Count > 0 && quickSlots[0] != null)
        {
            SwitchToQuickSlot(0);
        }

        if (orbitController != null && allCores.Count > 0)
        {
            orbitController.InitializeOrbits();
        }
    }

    void InitializeQuickSlots()
    {
        quickSlots = new CoreData[quickSlotCount];

        // Заполняем быстрые слоты первыми ядрами из инвентаря
        for (int i = 0; i < Mathf.Min(quickSlotCount, allCores.Count); i++)
        {
            quickSlots[i] = allCores[i];
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Переключение быстрых слотов 1, 2, 3
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToQuickSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToQuickSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToQuickSlot(2);

        // Использование способностей активного ядра
        if (activeCore != null)
        {
            if (Input.GetKeyDown(KeyCode.E) && activeCore.EAbility != null)
            {
                ExecuteAbility(activeCore.EAbility, "E");
            }

            if (Input.GetKeyDown(KeyCode.Q) && activeCore.QAbility != null)
            {
                ExecuteAbility(activeCore.QAbility, "Q");
            }
        }
    }

    // Метод для выполнения способностей
    private void ExecuteAbility(CoreAbility ability, string abilityKey)
    {
        if (ability.CanExecute())
        {
            Debug.Log($"Executing {abilityKey} ability: {ability.abilityName}");
            ability.ExecuteAbility();
        }
        else
        {
            Debug.LogWarning($"Cannot execute {abilityKey} ability: {ability.abilityName}");
        }
    }

    public void SwitchToQuickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount)
        {
            Debug.LogWarning($"Неверный индекс слота: {slotIndex}");
            return;
        }

        if (quickSlots[slotIndex] == null)
        {
            Debug.LogWarning($"Слот {slotIndex} пустой");
            return;
        }

        activeQuickSlotIndex = slotIndex;
        SetActiveCore(quickSlots[slotIndex]);
    }

    public void SetActiveCore(CoreData newCore)
    {
        if (newCore == activeCore) return;

        ResetCoreEffects();
        activeCore = newCore;
        ApplyCoreEffects();

        OnCoreChanged?.Invoke(activeCore);

        Debug.Log($"Активное ядро изменено на: {activeCore.coreName}");
        if (activeCore.EAbility != null)
        {
            Debug.Log($"E Ability: {activeCore.EAbility.abilityName}, Initialized: {activeCore.EAbility.CanExecute()}");
        }
        if (activeCore.QAbility != null)
        {
            Debug.Log($"Q Ability: {activeCore.QAbility.abilityName}, Initialized: {activeCore.QAbility.CanExecute()}");
        }
    }

    private void OnActiveCoreChanged(CoreData newActiveCore)
    {
        if (orbitController != null)
        {
            orbitController.OnActiveCoreChanged(newActiveCore);
        }
    }

    void ResetCoreEffects()
    {
        if (activeCore == null || slimeController == null) return;

        // Восстанавливаем оригинальные значения
        slimeController.currentMass /= (1 + activeCore.massMultiplier);
        slimeController.moveSpeed /= (1 + activeCore.speedMultiplier);
        slimeController.middleStiffness /= (1 + activeCore.middleStiffnessMultiplier);
        slimeController.surfaceStiffness /= (1 + activeCore.surfaceStiffnessMultiplier);
    }

    void ApplyCoreEffects()
    {
        if (activeCore == null || slimeController == null) return;

        // Применяем модификаторы к физике слизи
        slimeController.currentMass *= (1 + activeCore.massMultiplier);
        slimeController.moveSpeed *= (1 + activeCore.speedMultiplier);
        slimeController.middleStiffness *= (1 + activeCore.middleStiffnessMultiplier);
        slimeController.surfaceStiffness *= (1 + activeCore.surfaceStiffnessMultiplier);

        // Инициализируем способности
        InitializeAbilities();

        Debug.Log($"Применены эффекты ядра: {activeCore.coreName}");
    }

    private void InitializeAbilities()
    {
        if (activeCore == null || slimeController == null) return;

        if (activeCore.EAbility != null)
        {
            activeCore.EAbility.Initialize(slimeController, this);
            Debug.Log($"E ability initialized: {activeCore.EAbility.abilityName}");
        }

        if (activeCore.QAbility != null)
        {
            activeCore.QAbility.Initialize(slimeController, this);
            Debug.Log($"Q ability initialized: {activeCore.QAbility.abilityName}");
        }
    }

    // Метод для отладки получения slimeController из способности
    private SlimeCharacterController GetSlimeControllerForAbility(CoreAbility ability)
    {
        // Используем рефлексию для доступа к protected полю
        var field = typeof(CoreAbility).GetField("slimeController",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(ability) as SlimeCharacterController;
    }

    public void AddCore(CoreData core)
    {
        if (allCores.Count >= maxCores)
        {
            Debug.LogWarning("Инвентарь ядер полон!");
            return;
        }

        if (!allCores.Contains(core))
        {
            allCores.Add(core);

            // Добавляем ядро на орбиту
            if (orbitController != null)
            {
                orbitController.AddCoreToOrbit(core);
            }

            // Если есть пустые быстрые слоты, автоматически назначаем ядро
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == null)
                {
                    AssignCoreToQuickSlot(core, i);
                    break;
                }
            }

            Debug.Log($"Добавлено ядро: {core.coreName}");
        }
    }

    public void RemoveCore(CoreData core)
    {
        if (allCores.Contains(core))
        {
            // Если удаляемое ядро активно, переключаемся на другое
            if (activeCore == core)
            {
                // Ищем другое ядро для активации
                CoreData newActiveCore = null;
                foreach (CoreData otherCore in allCores)
                {
                    if (otherCore != core)
                    {
                        newActiveCore = otherCore;
                        break;
                    }
                }

                if (newActiveCore != null)
                {
                    SetActiveCore(newActiveCore);
                }
                else
                {
                    activeCore = null;
                    ResetCoreEffects();
                }
            }

            // Удаляем из быстрых слотов
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == core)
                {
                    quickSlots[i] = null;
                }
            }

            allCores.Remove(core);

            // Удаляем ядро с орбиты
            if (orbitController != null)
            {
                orbitController.RemoveCoreFromOrbit(core);
            }

            OnQuickSlotsUpdated?.Invoke();

            Debug.Log($"Удалено ядро: {core.coreName}");
        }
    }

    public void AssignCoreToQuickSlot(CoreData core, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount)
        {
            Debug.LogWarning($"Неверный индекс слота: {slotIndex}");
            return;
        }

        quickSlots[slotIndex] = core;
        OnQuickSlotsUpdated?.Invoke();

        Debug.Log($"Ядро {core.coreName} назначено на слот {slotIndex + 1}");

        // Если слот активный, сразу применяем эффекты
        if (slotIndex == activeQuickSlotIndex)
        {
            SetActiveCore(core);
        }
    }

    public void ClearQuickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlotCount) return;

        // Если очищаем активный слот, деактивируем ядро
        if (slotIndex == activeQuickSlotIndex && quickSlots[slotIndex] != null)
        {
            ResetCoreEffects();
            activeCore = null;
            OnCoreChanged?.Invoke(null);
        }

        quickSlots[slotIndex] = null;
        OnQuickSlotsUpdated?.Invoke();

        Debug.Log($"Слот {slotIndex + 1} очищен");
    }

    public bool HasCore(string coreID)
    {
        foreach (CoreData core in allCores)
        {
            if (core.coreID == coreID)
            {
                return true;
            }
        }
        return false;
    }

    public CoreData GetCoreByID(string coreID)
    {
        foreach (CoreData core in allCores)
        {
            if (core.coreID == coreID)
            {
                return core;
            }
        }
        return null;
    }

    public int GetCoreCount()
    {
        return allCores.Count;
    }

    public bool IsInventoryFull()
    {
        return allCores.Count >= maxCores;
    }

    // Метод для принудительного обновления UI
    public void RefreshUI()
    {
        OnCoreChanged?.Invoke(activeCore);
        OnQuickSlotsUpdated?.Invoke();
    }

    // Метод для сброса всего инвентаря (для тестирования)
    public void ResetInventory()
    {
        // Сбрасываем активное ядро
        if (activeCore != null)
        {
            ResetCoreEffects();
            activeCore = null;
        }

        // Очищаем списки
        allCores.Clear();
        quickSlots = new CoreData[quickSlotCount];
        activeQuickSlotIndex = 0;

        // Очищаем орбиты
        if (orbitController != null)
        {
            orbitController.ClearOrbits();
        }

        // Обновляем UI
        RefreshUI();

        Debug.Log("Инвентарь ядер сброшен");
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (activeCore != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawIcon(transform.position + Vector3.up * 0.7f, "CoreIcon", true);
        }
    }
}