using UnityEngine;

[CreateAssetMenu(fileName = "New Core", menuName = "Slime Cores/Core Data")]
public class CoreData : ScriptableObject
{
    [Header("Основная информация")]
    public string coreID = "core_stone";
    public string coreName = "Ядро-камня";
    public CoreRarity rarity = CoreRarity.Common;
    public int version = 1;

    [Header("Визуальные настройки")]
    public Sprite inventoryIcon;
    public GameObject worldModel;
    public Color coreColor = Color.white;

    [Header("Настройки орбиты")]
    [Range(0.001f, 3.0f)] // Изменено с 0.1f на 0.025f
    public float orbitScale = 1.0f;
    [Range(0.1f, 3.0f)]
    public float orbitRadiusMultiplier = 1.0f;

    [Header("Влияние на физику слизи")]
    [Range(-0.5f, 0.5f)]
    public float massMultiplier = 0f;
    [Range(-0.5f, 0.5f)]
    public float speedMultiplier = 0f;
    [Range(0f, 1f)]
    public float inertiaMultiplier = 0f;
    [Range(-0.5f, 0.5f)]
    public float middleStiffnessMultiplier = 0f;
    [Range(-0.5f, 0.5f)]
    public float surfaceStiffnessMultiplier = 0f;

    [Header("Способности")]
    public CoreAbility EAbility;
    public CoreAbility QAbility;
}

public enum CoreRarity
{
    Common,
    Special
}   