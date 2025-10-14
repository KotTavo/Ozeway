using UnityEngine;

public class PanelController : MonoBehaviour
{
    [Header("Panels")]
    public RectTransform inventoryPanel;
    public RectTransform bottomPanel; // карта
    public RectTransform coreSystemPanel;

    [Header("HUD")]
    public RectTransform hudPanel;

    [Header("Speeds")]
    public float inventorySpeed = 5f;
    public float bottomPanelSpeed = 5f;
    public float corePanelSpeed = 5f;
    public float hudSpeed = 5f;

    [Header("Offsets")]
    public float bottomPanelMoveDistance = 1200f;
    public float corePanelMoveDistance = 1000f;
    public float hudHiddenOffset = -200f; // смещение HUD вниз при скрытии

    private enum PanelType { None, Inventory, Bottom, CoreSystem }
    private PanelType activePanel = PanelType.None;

    // Позиции
    private Vector2 invClosedPos;
    private Vector2 invOpenPos;
    private Vector2 bottomClosedPos;
    private Vector2 bottomOpenPos;
    private Vector2 coreClosedPos;
    private Vector2 coreOpenPos;
    private Vector2 hudBasePos;
    private Vector2 hudTargetPos;

    void Start()
    {
        // Inventory
        invClosedPos = new Vector2(-inventoryPanel.rect.width, inventoryPanel.anchoredPosition.y);
        invOpenPos = new Vector2(0, inventoryPanel.anchoredPosition.y);
        inventoryPanel.anchoredPosition = invClosedPos;

        // Bottom Panel (Map)
        bottomClosedPos = bottomPanel.anchoredPosition;
        bottomOpenPos = bottomClosedPos + new Vector2(0, bottomPanelMoveDistance);
        bottomPanel.anchoredPosition = bottomClosedPos;

        // Core System Panel
        coreClosedPos = coreSystemPanel.anchoredPosition;
        coreOpenPos = coreClosedPos + new Vector2(corePanelMoveDistance, 0);
        coreSystemPanel.anchoredPosition = coreClosedPos;

        // HUD
        hudBasePos = hudPanel.anchoredPosition;
        hudTargetPos = hudBasePos;
    }

    void Update()
    {
        // Клавиши для переключения панели
        if (Input.GetKeyDown(KeyCode.I)) TogglePanel(PanelType.Inventory);
        if (Input.GetKeyDown(KeyCode.M)) TogglePanel(PanelType.Bottom);
        if (Input.GetKeyDown(KeyCode.C)) TogglePanel(PanelType.CoreSystem);

        // Плавное движение Inventory
        inventoryPanel.anchoredPosition = Vector2.Lerp(
            inventoryPanel.anchoredPosition,
            activePanel == PanelType.Inventory ? invOpenPos : invClosedPos,
            Time.deltaTime * inventorySpeed
        );

        // Плавное движение Bottom Panel
        bottomPanel.anchoredPosition = Vector2.Lerp(
            bottomPanel.anchoredPosition,
            activePanel == PanelType.Bottom ? bottomOpenPos : bottomClosedPos,
            Time.deltaTime * bottomPanelSpeed
        );

        // Плавное движение Core System Panel
        coreSystemPanel.anchoredPosition = Vector2.Lerp(
            coreSystemPanel.anchoredPosition,
            activePanel == PanelType.CoreSystem ? coreOpenPos : coreClosedPos,
            Time.deltaTime * corePanelSpeed
        );

        // Плавное движение HUD
        // HUD скрывается, если любая панель открыта
        bool anyPanelOpen = activePanel != PanelType.None;

        hudTargetPos = anyPanelOpen ? hudBasePos + new Vector2(0, hudHiddenOffset) : hudBasePos;

        hudPanel.anchoredPosition = Vector2.Lerp(
            hudPanel.anchoredPosition,
            hudTargetPos,
            Time.deltaTime * hudSpeed
        );
    }

    void TogglePanel(PanelType panel)
    {
        if (activePanel == panel)
        {
            // если эта панель уже открыта, закрываем её
            activePanel = PanelType.None;
        }
        else
        {
            // открываем выбранную панель, все остальные закрываются автоматически
            activePanel = panel;
        }
    }
}
