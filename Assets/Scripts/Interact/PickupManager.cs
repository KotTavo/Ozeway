// PickupManager.cs
using UnityEngine;

public static class PickupManager
{
    public static float GlobalScaleReduction { get; set; } = 0.2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Установка значений по умолчанию
        GlobalScaleReduction = 0.2f;
    }
}