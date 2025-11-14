using UnityEngine;
using UnityEngine.UI;

public class CoreSlotUI : MonoBehaviour
{
    [Header("UI элементы")]
    public Image coreIcon;
    public Text coreName;
    public Image rarityIndicator;
    public GameObject selectedIndicator;
    public Button slotButton;

    [Header("Цвета редкости")]
    public Color commonColor = Color.white;
    public Color specialColor = Color.yellow;

    public CoreData assignedCore { get; private set; }
    private CoreUIManager uiManager;
    private bool isQuickSlot = false;

    public void Initialize(CoreData core, CoreUIManager manager)
    {
        assignedCore = core;
        uiManager = manager;

        UpdateUI();

        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    public void AssignCore(CoreData core)
    {
        assignedCore = core;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (assignedCore != null)
        {
            // Устанавливаем иконку
            if (coreIcon != null)
            {
                if (assignedCore.inventoryIcon != null)
                {
                    coreIcon.sprite = assignedCore.inventoryIcon;
                    coreIcon.color = Color.white;
                }
                else
                {
                    coreIcon.sprite = null;
                    coreIcon.color = assignedCore.coreColor;
                }
            }

            // Устанавливаем название (только для обычных слотов)
            if (coreName != null && !isQuickSlot)
            {
                coreName.text = assignedCore.coreName;
            }

            // Устанавливаем индикатор редкости
            if (rarityIndicator != null && !isQuickSlot)
            {
                rarityIndicator.color = assignedCore.rarity == CoreRarity.Common ? commonColor : specialColor;
            }
        }
        else
        {
            // Очищаем слот
            if (coreIcon != null)
            {
                coreIcon.sprite = null;
                coreIcon.color = Color.gray;
            }

            if (coreName != null && !isQuickSlot)
            {
                coreName.text = "Пусто";
            }

            if (rarityIndicator != null && !isQuickSlot)
            {
                rarityIndicator.color = Color.gray;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }
    }

    public void SetAsQuickSlot(bool isQuick)
    {
        isQuickSlot = isQuick;
    }

    void OnSlotClicked()
    {
        if (assignedCore != null)
        {
            if (isQuickSlot)
            {
                // Для быстрого слота - переключаемся на него
                int slotIndex = FindQuickSlotIndex();
                if (slotIndex >= 0)
                {
                    uiManager.OnQuickSlotSelected(slotIndex);
                }
            }
            else
            {
                // Для обычного слота - выбираем ядро
                uiManager.OnCoreSelected(assignedCore);
            }
        }
        else if (isQuickSlot)
        {
            // Если быстрый слот пустой - открываем выбор ядер
            uiManager.ToggleCoreSelection();
        }
    }

    private int FindQuickSlotIndex()
    {
        if (uiManager == null || uiManager.quickSlotsUI == null) return -1;

        for (int i = 0; i < uiManager.quickSlotsUI.Length; i++)
        {
            if (uiManager.quickSlotsUI[i] == this)
            {
                return i;
            }
        }
        return -1;
    }
}