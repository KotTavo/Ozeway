using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CoreUIManager : MonoBehaviour
{
    [Header("Настройки UI")]
    public int quickSlotCount = 3;
    public Sprite defaultCoreIcon;
    public Color commonCoreColor = Color.white;
    public Color specialCoreColor = Color.yellow;

    [Header("Масштабирование")]
    [Range(0.1f, 2f)]
    public float uiScale = 1f;
    public float referenceResolutionHeight = 1080f;

    [Header("Автоматические ссылки")]
    public GameObject coreSelectionPanel;
    public Transform coresGrid;
    public CoreSlotUI[] quickSlotsUI;

    private CoreInventory coreInventory;
    private bool isUIOpen = false;
    private Canvas canvas;
    private GameObject quickSlotsPanel;

    // Префабы создаваемые в runtime
    private GameObject coreSlotPrefab;
    private GameObject quickSlotPrefab;
    private Font defaultFont;

    // Адаптивные размеры
    private float scaledPanelWidth;
    private float scaledPanelHeight;
    private float scaledSlotSize;
    private float scaledQuickSlotWidth;
    private float scaledQuickSlotHeight;

    void Start()
    {
        coreInventory = FindObjectOfType<CoreInventory>();

        if (coreInventory != null)
        {
            coreInventory.OnCoreChanged += UpdateActiveCoreUI;
            coreInventory.OnQuickSlotsUpdated += UpdateQuickSlotsUI;
        }

        // Рассчитываем адаптивные размеры
        CalculateAdaptiveSizes();

        // Получаем шрифт по умолчанию
        GetDefaultFont();

        CreateCanvas();
        CreateCoreSelectionUI();
        CreateQuickSlotsUI();
        InitializeUI();

        // Скрываем панель выбора при старте
        if (coreSelectionPanel != null)
            coreSelectionPanel.SetActive(false);
    }

    void CalculateAdaptiveSizes()
    {
        // Базовые размеры для разрешения 1080p
        float basePanelWidth = 400f;
        float basePanelHeight = 300f;
        float baseSlotSize = 80f;
        float baseQuickSlotWidth = 70f;
        float baseQuickSlotHeight = 50f;

        // Масштабируем относительно текущего разрешения
        float screenScale = (float)Screen.height / referenceResolutionHeight;
        float totalScale = screenScale * uiScale;

        scaledPanelWidth = basePanelWidth * totalScale;
        scaledPanelHeight = basePanelHeight * totalScale;
        scaledSlotSize = baseSlotSize * totalScale;
        scaledQuickSlotWidth = baseQuickSlotWidth * totalScale;
        scaledQuickSlotHeight = baseQuickSlotHeight * totalScale;

        Debug.Log($"Адаптивные размеры UI: экран={Screen.height}px, масштаб={totalScale:F2}");
    }

    void GetDefaultFont()
    {
        // Пытаемся найти любой доступный шрифт
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        if (fonts.Length > 0)
        {
            defaultFont = fonts[0];
        }
        else
        {
            // Создаем fallback - используем шрифт из системного Text компонента
            GameObject tempText = new GameObject("TempText");
            Text textComponent = tempText.AddComponent<Text>();
            defaultFont = textComponent.font;
            Destroy(tempText);
        }
    }

    void CreateCanvas()
    {
        // Ищем существующий Canvas или создаем новый
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("CoreSystemCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Добавляем CanvasScaler для адаптивности
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    void CreateCoreSelectionUI()
    {
        // Создаем панель выбора ядер
        coreSelectionPanel = new GameObject("CoreSelectionPanel");
        coreSelectionPanel.transform.SetParent(canvas.transform);

        // Добавляем компоненты для панели
        Image panelImage = coreSelectionPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Настраиваем RectTransform с адаптивными размерами
        RectTransform panelRect = coreSelectionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(scaledPanelWidth, scaledPanelHeight);
        panelRect.anchoredPosition = Vector2.zero;

        // Добавляем Vertical Layout Group
        VerticalLayoutGroup verticalLayout = coreSelectionPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;

        // Создаем заголовок
        CreateTitlePanel(coreSelectionPanel.transform, "ВЫБОР ЯДЕР");

        // Создаем сетку для ядер
        GameObject gridObject = new GameObject("CoresGrid");
        gridObject.transform.SetParent(coreSelectionPanel.transform);
        coresGrid = gridObject.transform;

        // Настраиваем Grid Layout Group для сетки с адаптивными размерами
        GridLayoutGroup gridLayout = gridObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(scaledSlotSize, scaledSlotSize);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;

        // Настраиваем размер сетки
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(scaledPanelWidth - 40, scaledPanelHeight - 80);

        // Создаем префаб слота ядра
        CreateCoreSlotPrefab();
    }

    void CreateTitlePanel(Transform parent, string titleText)
    {
        GameObject titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(parent);

        // Настраиваем layout
        HorizontalLayoutGroup layout = titlePanel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        RectTransform titleRect = titlePanel.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(scaledPanelWidth - 20, 30 * uiScale);

        // Создаем текст заголовка
        GameObject titleObject = new GameObject("TitleText");
        titleObject.transform.SetParent(titlePanel.transform);

        Text text = titleObject.AddComponent<Text>();
        text.text = titleText;

        if (defaultFont != null)
            text.font = defaultFont;

        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = Mathf.RoundToInt(18 * uiScale);
        text.fontStyle = FontStyle.Bold;

        RectTransform textRect = titleObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200 * uiScale, 25 * uiScale);
    }

    void CreateQuickSlotsUI()
    {
        // Создаем панель быстрых слотов
        quickSlotsPanel = new GameObject("QuickSlotsPanel");
        quickSlotsPanel.transform.SetParent(canvas.transform);

        // Настраиваем панель
        Image panelImage = quickSlotsPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panelRect = quickSlotsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(scaledQuickSlotWidth * quickSlotCount + 40, scaledQuickSlotHeight + 20);
        panelRect.anchoredPosition = new Vector2(0, 20 * uiScale);

        // Добавляем Horizontal Layout Group
        HorizontalLayoutGroup horizontalLayout = quickSlotsPanel.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(10, 10, 5, 5);
        horizontalLayout.spacing = 10 * uiScale;
        horizontalLayout.childAlignment = TextAnchor.MiddleCenter;

        // Создаем быстрые слоты
        quickSlotsUI = new CoreSlotUI[quickSlotCount];
        CreateQuickSlotPrefab();

        for (int i = 0; i < quickSlotCount; i++)
        {
            GameObject slotObject = Instantiate(quickSlotPrefab, quickSlotsPanel.transform);
            slotObject.name = $"QuickSlot_{i + 1}";

            // Добавляем текст с номером слота
            CreateSlotIndex(slotObject.transform, i + 1);

            quickSlotsUI[i] = slotObject.GetComponent<CoreSlotUI>();
            if (quickSlotsUI[i] != null)
            {
                quickSlotsUI[i].SetAsQuickSlot(true);
            }
        }
    }

    void CreateSlotIndex(Transform parent, int slotNumber)
    {
        GameObject indexObject = new GameObject("SlotIndex");
        indexObject.transform.SetParent(parent);

        Text indexText = indexObject.AddComponent<Text>();
        indexText.text = slotNumber.ToString();

        if (defaultFont != null)
            indexText.font = defaultFont;

        indexText.color = Color.white;
        indexText.alignment = TextAnchor.UpperRight;
        indexText.fontSize = Mathf.RoundToInt(12 * uiScale);
        indexText.fontStyle = FontStyle.Bold;

        RectTransform textRect = indexObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(1, 1);
        textRect.anchoredPosition = new Vector2(-2 * uiScale, -2 * uiScale);
        textRect.sizeDelta = new Vector2(15 * uiScale, 15 * uiScale);
    }

    void CreateCoreSlotPrefab()
    {
        coreSlotPrefab = new GameObject("CoreSlotPrefab");
        coreSlotPrefab.SetActive(false);

        // Настраиваем RectTransform с адаптивным размером
        RectTransform rect = coreSlotPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(scaledSlotSize, scaledSlotSize);

        // Добавляем фон
        Image background = coreSlotPrefab.AddComponent<Image>();
        background.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Добавляем иконку
        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(coreSlotPrefab.transform);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Добавляем текст названия
        GameObject nameObject = new GameObject("Name");
        nameObject.transform.SetParent(coreSlotPrefab.transform);
        Text nameText = nameObject.AddComponent<Text>();

        if (defaultFont != null)
            nameText.font = defaultFont;

        nameText.color = Color.white;
        nameText.alignment = TextAnchor.LowerCenter;
        nameText.fontSize = Mathf.RoundToInt(8 * uiScale);
        nameText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.3f);
        nameRect.offsetMin = new Vector2(2 * uiScale, 0);
        nameRect.offsetMax = new Vector2(-2 * uiScale, 0);

        // Добавляем индикатор редкости
        GameObject rarityObject = new GameObject("RarityIndicator");
        rarityObject.transform.SetParent(coreSlotPrefab.transform);
        Image rarityImage = rarityObject.AddComponent<Image>();
        rarityImage.color = commonCoreColor;

        RectTransform rarityRect = rarityObject.GetComponent<RectTransform>();
        rarityRect.anchorMin = new Vector2(0, 0.85f);
        rarityRect.anchorMax = new Vector2(1, 0.9f);
        rarityRect.offsetMin = new Vector2(5 * uiScale, 0);
        rarityRect.offsetMax = new Vector2(-5 * uiScale, 0);

        // Добавляем индикатор выделения
        GameObject selectedObject = new GameObject("SelectedIndicator");
        selectedObject.transform.SetParent(coreSlotPrefab.transform);
        Image selectedImage = selectedObject.AddComponent<Image>();
        selectedImage.color = Color.green;
        selectedObject.SetActive(false);

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
        selectedRect.anchorMin = Vector2.zero;
        selectedRect.anchorMax = Vector2.one;
        selectedRect.offsetMin = Vector2.zero;
        selectedRect.offsetMax = Vector2.zero;

        // Добавляем кнопку
        Button button = coreSlotPrefab.AddComponent<Button>();
        button.targetGraphic = background;

        // Настраиваем цвета кнопки
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        button.colors = colors;

        // Добавляем компонент CoreSlotUI
        CoreSlotUI slotUI = coreSlotPrefab.AddComponent<CoreSlotUI>();
        slotUI.coreIcon = iconImage;
        slotUI.coreName = nameText;
        slotUI.rarityIndicator = rarityImage;
        slotUI.selectedIndicator = selectedObject;
        slotUI.slotButton = button;
    }

    void CreateQuickSlotPrefab()
    {
        quickSlotPrefab = new GameObject("QuickSlotPrefab");
        quickSlotPrefab.SetActive(false);

        // Настраиваем RectTransform с адаптивным размером
        RectTransform rect = quickSlotPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(scaledQuickSlotWidth, scaledQuickSlotHeight);

        // Добавляем фон
        Image background = quickSlotPrefab.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Добавляем иконку
        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(quickSlotPrefab.transform);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Добавляем индикатор выделения
        GameObject selectedObject = new GameObject("SelectedIndicator");
        selectedObject.transform.SetParent(quickSlotPrefab.transform);
        Image selectedImage = selectedObject.AddComponent<Image>();
        selectedImage.color = Color.yellow;
        selectedObject.SetActive(false);

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
        selectedRect.anchorMin = Vector2.zero;
        selectedRect.anchorMax = Vector2.one;
        selectedRect.offsetMin = Vector2.zero;
        selectedRect.offsetMax = Vector2.zero;

        // Добавляем кнопку
        Button button = quickSlotPrefab.AddComponent<Button>();
        button.targetGraphic = background;

        // Добавляем компонент CoreSlotUI
        CoreSlotUI slotUI = quickSlotPrefab.AddComponent<CoreSlotUI>();
        slotUI.coreIcon = iconImage;
        slotUI.selectedIndicator = selectedObject;
        slotUI.slotButton = button;
    }

    void InitializeUI()
    {
        if (coresGrid == null || coreInventory == null) return;

        // Очищаем сетку
        foreach (Transform child in coresGrid)
        {
            Destroy(child.gameObject);
        }

        // Создаем слоты для всех ядер
        foreach (CoreData core in coreInventory.allCores)
        {
            GameObject slotObject = Instantiate(coreSlotPrefab, coresGrid);
            slotObject.SetActive(true);
            slotObject.name = $"CoreSlot_{core.coreName}";

            CoreSlotUI slotUI = slotObject.GetComponent<CoreSlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(core, this);
            }
        }

        UpdateQuickSlotsUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleCoreSelection();
        }
    }

    public void ToggleCoreSelection()
    {
        isUIOpen = !isUIOpen;

        if (coreSelectionPanel != null)
        {
            coreSelectionPanel.SetActive(isUIOpen);

            // Обновляем UI при открытии
            if (isUIOpen)
            {
                UpdateQuickSlotsUI();
            }
        }
    }

    void UpdateActiveCoreUI(CoreData activeCore)
    {
        if (quickSlotsUI == null) return;

        for (int i = 0; i < quickSlotsUI.Length; i++)
        {
            if (quickSlotsUI[i] != null)
            {
                bool isSelected = (coreInventory != null && i == coreInventory.activeQuickSlotIndex);
                quickSlotsUI[i].SetSelected(isSelected);
            }
        }
    }

    void UpdateQuickSlotsUI()
    {
        if (coreInventory == null || quickSlotsUI == null) return;

        for (int i = 0; i < quickSlotsUI.Length; i++)
        {
            if (quickSlotsUI[i] != null)
            {
                CoreData slotCore = coreInventory.quickSlots[i];
                quickSlotsUI[i].AssignCore(slotCore);

                bool isSelected = (i == coreInventory.activeQuickSlotIndex);
                quickSlotsUI[i].SetSelected(isSelected);
            }
        }
    }

    public void OnCoreSelected(CoreData core)
    {
        if (coreInventory == null) return;

        // Находим слот для назначения (первый пустой или текущий активный)
        for (int i = 0; i < coreInventory.quickSlotCount; i++)
        {
            if (coreInventory.quickSlots[i] == null || i == coreInventory.activeQuickSlotIndex)
            {
                coreInventory.AssignCoreToQuickSlot(core, i);
                coreInventory.SwitchToQuickSlot(i);
                break;
            }
        }

        // Закрываем панель выбора после выбора
        ToggleCoreSelection();
    }

    public void OnQuickSlotSelected(int slotIndex)
    {
        if (coreInventory != null)
        {
            coreInventory.SwitchToQuickSlot(slotIndex);
        }
    }
}