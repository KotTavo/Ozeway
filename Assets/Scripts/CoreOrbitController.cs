using System.Collections.Generic;
using UnityEngine;

public class CoreOrbitController : MonoBehaviour
{
    [Header("Настройки орбиты")]
    public float baseOrbitRadius = 1.5f;
    public float rotationSpeed = 90f;
    public bool clockwise = true;

    [Header("Настройки размера ядер")]
    [Range(0.025f, 3.0f)]
    public float globalCoreScale = 1.0f;
    public bool scaleWithMass = true;
    public float sizeReductionMultiplier = 0.1f;

    [Header("Настройки анимации центра")]
    public float centerTransitionSpeed = 5f;
    public float centerScaleMultiplier = 1.5f;

    [Header("Ссылки")]
    public CoreInventory coreInventory;
    public List<GameObject> orbitingCores = new List<GameObject>();

    private List<Transform> coreTransforms = new List<Transform>();
    private List<float> coreAngles = new List<float>();
    private List<CoreData> coreDataList = new List<CoreData>();
    private List<bool> isMovingToCenter = new List<bool>();
    private List<bool> isMovingToOrbit = new List<bool>();
    private List<float> transitionProgress = new List<float>();
    private List<Vector3> transitionStartPos = new List<Vector3>();
    private List<Vector3> transitionTargetPos = new List<Vector3>();
    private List<Vector3> transitionStartScale = new List<Vector3>();
    private List<Vector3> transitionTargetScale = new List<Vector3>();

    private CoreData previousActiveCore = null;

    void Start()
    {
        if (coreInventory == null)
            coreInventory = GetComponent<CoreInventory>();

        InitializeOrbits();
    }

    void Update()
    {
        UpdateOrbits();
        UpdateCoreTransitions();
    }

    public void InitializeOrbits()
    {
        ClearOrbits();

        for (int i = 0; i < coreInventory.allCores.Count; i++)
        {
            CreateOrbitingCore(coreInventory.allCores[i], i);
        }

        // Если есть активное ядро, перемещаем его в центр
        if (coreInventory.activeCore != null)
        {
            int activeIndex = coreDataList.IndexOf(coreInventory.activeCore);
            if (activeIndex >= 0)
            {
                MoveCoreToCenter(activeIndex);
            }
        }
    }

    public void ClearOrbits()
    {
        foreach (GameObject core in orbitingCores)
        {
            if (core != null)
                Destroy(core);
        }
        orbitingCores.Clear();
        coreTransforms.Clear();
        coreAngles.Clear();
        coreDataList.Clear();
        isMovingToCenter.Clear();
        isMovingToOrbit.Clear();
        transitionProgress.Clear();
        transitionStartPos.Clear();
        transitionTargetPos.Clear();
        transitionStartScale.Clear();
        transitionTargetScale.Clear();
    }

    void CreateOrbitingCore(CoreData coreData, int index)
    {
        if (coreData.worldModel == null)
        {
            Debug.LogWarning($"У ядра {coreData.coreName} нет модели для отображения!");
            return;
        }

        GameObject coreInstance = Instantiate(coreData.worldModel, transform);
        coreInstance.name = $"OrbitingCore_{coreData.coreName}";

        // Применяем настройки размера из CoreData
        ApplyCoreScaling(coreInstance, coreData, index);

        orbitingCores.Add(coreInstance);
        coreTransforms.Add(coreInstance.transform);
        coreDataList.Add(coreData);

        // Инициализируем списки переходов
        isMovingToCenter.Add(false);
        isMovingToOrbit.Add(false);
        transitionProgress.Add(0f);
        transitionStartPos.Add(Vector3.zero);
        transitionTargetPos.Add(Vector3.zero);
        transitionStartScale.Add(Vector3.one);
        transitionTargetScale.Add(Vector3.one);

        // Равномерно распределяем ядра по орбите
        float angle = (360f / coreInventory.allCores.Count) * index;
        coreAngles.Add(angle);

        // Начальная позиция
        UpdateCorePosition(index);
    }

    void ApplyCoreScaling(GameObject coreInstance, CoreData coreData, int index)
    {
        float baseScale = coreData.orbitScale * sizeReductionMultiplier;
        float finalScale = baseScale * globalCoreScale;

        if (scaleWithMass)
        {
            float massFactor = 0.8f + (coreData.massMultiplier * 0.4f);
            finalScale *= massFactor;
        }

        finalScale = Mathf.Max(finalScale, 0.025f * sizeReductionMultiplier);
        coreInstance.transform.localScale = Vector3.one * finalScale;

        Collider2D collider = coreInstance.GetComponent<Collider2D>();
        if (collider != null && collider is CircleCollider2D circleCollider)
        {
            circleCollider.radius *= finalScale;
        }
    }

    void UpdateOrbits()
    {
        if (coreTransforms.Count == 0) return;

        float direction = clockwise ? 1f : -1f;
        float angleStep = rotationSpeed * Time.deltaTime * direction;

        for (int i = 0; i < coreAngles.Count; i++)
        {
            // Не обновляем угол для ядер, которые движутся к центру или от центра
            if (isMovingToCenter[i] || isMovingToOrbit[i]) continue;

            // Не обновляем угол для активного ядра (оно в центре)
            if (coreDataList[i] == coreInventory.activeCore) continue;

            coreAngles[i] += angleStep;
            if (coreAngles[i] >= 360f) coreAngles[i] -= 360f;
            if (coreAngles[i] < 0f) coreAngles[i] += 360f;

            UpdateCorePosition(i);
        }
    }

    void UpdateCoreTransitions()
    {
        for (int i = 0; i < coreTransforms.Count; i++)
        {
            if (isMovingToCenter[i])
            {
                transitionProgress[i] += Time.deltaTime * centerTransitionSpeed;
                float t = Mathf.Clamp01(transitionProgress[i]);

                // Плавное перемещение к центру
                coreTransforms[i].localPosition = Vector3.Lerp(
                    transitionStartPos[i],
                    transitionTargetPos[i],
                    t
                );

                // Плавное изменение масштаба
                coreTransforms[i].localScale = Vector3.Lerp(
                    transitionStartScale[i],
                    transitionTargetScale[i],
                    t
                );

                // Если достигли центра
                if (t >= 1f)
                {
                    isMovingToCenter[i] = false;
                    transitionProgress[i] = 0f;
                    coreTransforms[i].localPosition = Vector3.zero;
                }
            }
            else if (isMovingToOrbit[i])
            {
                transitionProgress[i] += Time.deltaTime * centerTransitionSpeed;
                float t = Mathf.Clamp01(transitionProgress[i]);

                // Плавное перемещение на орбиту
                coreTransforms[i].localPosition = Vector3.Lerp(
                    transitionStartPos[i],
                    transitionTargetPos[i],
                    t
                );

                // Плавное изменение масштаба
                coreTransforms[i].localScale = Vector3.Lerp(
                    transitionStartScale[i],
                    transitionTargetScale[i],
                    t
                );

                // Если достигли орбиты
                if (t >= 1f)
                {
                    isMovingToOrbit[i] = false;
                    transitionProgress[i] = 0f;
                    UpdateCorePosition(i);
                }
            }
        }
    }

    void UpdateCorePosition(int index)
    {
        if (index >= coreTransforms.Count) return;

        // Не обновляем позицию для ядер в переходе
        if (isMovingToCenter[index] || isMovingToOrbit[index]) return;

        // Активное ядро всегда в центре
        if (coreDataList[index] == coreInventory.activeCore)
        {
            coreTransforms[index].localPosition = Vector3.zero;
            return;
        }

        CoreData coreData = coreDataList[index];
        float radius = baseOrbitRadius * coreData.orbitRadiusMultiplier;

        float rad = coreAngles[index] * Mathf.Deg2Rad;
        Vector3 position = new Vector3(
            Mathf.Cos(rad) * radius,
            Mathf.Sin(rad) * radius,
            0f
        );

        coreTransforms[index].localPosition = position;

        // Обновляем масштаб (активное ядро больше)
        bool isActive = coreData == coreInventory.activeCore;
        coreTransforms[index].localScale = Vector3.one * GetCoreScale(coreData, isActive);
    }

    float GetCoreScale(CoreData coreData, bool isActive)
    {
        float baseScale = coreData.orbitScale * sizeReductionMultiplier * globalCoreScale;

        if (scaleWithMass)
        {
            float massFactor = 0.8f + (coreData.massMultiplier * 0.4f);
            baseScale *= massFactor;
        }

        // Увеличиваем масштаб активного ядра
        if (isActive)
        {
            baseScale *= centerScaleMultiplier;
        }

        baseScale = Mathf.Max(baseScale, 0.025f * sizeReductionMultiplier);
        return baseScale;
    }

    public void MoveCoreToCenter(int index)
    {
        if (index < 0 || index >= coreTransforms.Count) return;

        // Запоминаем предыдущее активное ядро
        previousActiveCore = coreInventory.activeCore;

        // Останавливаем все переходы для этого ядра
        isMovingToCenter[index] = false;
        isMovingToOrbit[index] = false;
        transitionProgress[index] = 0f;

        // Настраиваем переход к центру
        isMovingToCenter[index] = true;
        transitionStartPos[index] = coreTransforms[index].localPosition;
        transitionTargetPos[index] = Vector3.zero;
        transitionStartScale[index] = coreTransforms[index].localScale;
        transitionTargetScale[index] = Vector3.one * GetCoreScale(coreDataList[index], true);

        // Если было предыдущее активное ядро, перемещаем его на орбиту
        if (previousActiveCore != null && previousActiveCore != coreDataList[index])
        {
            int previousIndex = coreDataList.IndexOf(previousActiveCore);
            if (previousIndex >= 0)
            {
                MoveCoreToOrbit(previousIndex);
            }
        }

        Debug.Log($"Ядро {coreDataList[index].coreName} перемещается в центр");
    }

    public void MoveCoreToOrbit(int index)
    {
        if (index < 0 || index >= coreTransforms.Count) return;

        // Останавливаем все переходы для этого ядра
        isMovingToCenter[index] = false;
        isMovingToOrbit[index] = false;
        transitionProgress[index] = 0f;

        // Вычисляем целевую позицию на орбите
        CoreData coreData = coreDataList[index];
        float radius = baseOrbitRadius * coreData.orbitRadiusMultiplier;
        float targetAngle = coreAngles[index];
        Vector3 targetPosition = new Vector3(
            Mathf.Cos(targetAngle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(targetAngle * Mathf.Deg2Rad) * radius,
            0f
        );

        // Настраиваем переход на орбиту
        isMovingToOrbit[index] = true;
        transitionStartPos[index] = coreTransforms[index].localPosition;
        transitionTargetPos[index] = targetPosition;
        transitionStartScale[index] = coreTransforms[index].localScale;
        transitionTargetScale[index] = Vector3.one * GetCoreScale(coreData, false);

        Debug.Log($"Ядро {coreData.coreName} перемещается на орбиту");
    }

    public void OnActiveCoreChanged(CoreData newActiveCore)
    {
        if (newActiveCore == null)
        {
            // Если активное ядро сброшено, перемещаем все ядра на орбиты
            for (int i = 0; i < coreDataList.Count; i++)
            {
                MoveCoreToOrbit(i);
            }
            return;
        }

        int newActiveIndex = coreDataList.IndexOf(newActiveCore);
        if (newActiveIndex >= 0)
        {
            MoveCoreToCenter(newActiveIndex);
        }
    }

    public void AddCoreToOrbit(CoreData newCore)
    {
        int newIndex = coreInventory.allCores.Count - 1;
        CreateOrbitingCore(newCore, newIndex);
        RedistributeAngles();
    }

    void RedistributeAngles()
    {
        for (int i = 0; i < coreAngles.Count; i++)
        {
            coreAngles[i] = (360f / coreInventory.allCores.Count) * i;
        }
    }

    public void RemoveCoreFromOrbit(CoreData coreToRemove)
    {
        int indexToRemove = coreDataList.IndexOf(coreToRemove);
        if (indexToRemove >= 0)
        {
            if (orbitingCores[indexToRemove] != null)
                Destroy(orbitingCores[indexToRemove]);

            orbitingCores.RemoveAt(indexToRemove);
            coreTransforms.RemoveAt(indexToRemove);
            coreAngles.RemoveAt(indexToRemove);
            coreDataList.RemoveAt(indexToRemove);
            isMovingToCenter.RemoveAt(indexToRemove);
            isMovingToOrbit.RemoveAt(indexToRemove);
            transitionProgress.RemoveAt(indexToRemove);
            transitionStartPos.RemoveAt(indexToRemove);
            transitionTargetPos.RemoveAt(indexToRemove);
            transitionStartScale.RemoveAt(indexToRemove);
            transitionTargetScale.RemoveAt(indexToRemove);

            RedistributeAngles();
        }
    }

    public void UpdateCoreScaling()
    {
        for (int i = 0; i < orbitingCores.Count; i++)
        {
            if (orbitingCores[i] != null)
            {
                ApplyCoreScaling(orbitingCores[i], coreDataList[i], i);
                UpdateCorePosition(i);
            }
        }
    }

    public void SetGlobalCoreScale(float newScale)
    {
        globalCoreScale = Mathf.Clamp(newScale, 0.025f, 3.0f);
        UpdateCoreScaling();
    }

    public void SetSizeReductionMultiplier(float newMultiplier)
    {
        sizeReductionMultiplier = Mathf.Clamp(newMultiplier, 0.01f, 1.0f);
        UpdateCoreScaling();
    }

    public void SetBaseOrbitRadius(float newRadius)
    {
        baseOrbitRadius = Mathf.Clamp(newRadius, 0.5f, 5.0f);
        UpdateCoreScaling();
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }

    public void SetCenterTransitionSpeed(float newSpeed)
    {
        centerTransitionSpeed = Mathf.Clamp(newSpeed, 1f, 20f);
    }

    public string GetOrbitInfo()
    {
        int activeCores = 0;
        foreach (var core in coreDataList)
        {
            if (core == coreInventory.activeCore) activeCores++;
        }

        return $"Ядер: {orbitingCores.Count}, Активных: {activeCores}, Переходов: {isMovingToCenter.Count}";
    }

    void OnDrawGizmosSelected()
    {
        if (coreInventory == null || coreInventory.allCores.Count == 0) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, baseOrbitRadius * 0.5f);
        Gizmos.DrawWireSphere(transform.position, baseOrbitRadius);
        Gizmos.DrawWireSphere(transform.position, baseOrbitRadius * 1.5f);

        // Центр - красный круг
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        Gizmos.color = Color.green;
        for (int i = 0; i < coreInventory.allCores.Count; i++)
        {
            CoreData core = coreInventory.allCores[i];
            float radius = baseOrbitRadius * core.orbitRadiusMultiplier;
            float angle = (360f / coreInventory.allCores.Count) * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 position = transform.position + new Vector3(
                Mathf.Cos(rad) * radius,
                Mathf.Sin(rad) * radius,
                0f
            );

            float displaySize = 0.1f * core.orbitScale * globalCoreScale * sizeReductionMultiplier;
            Gizmos.DrawWireSphere(position, displaySize);

            // Линия к центру
            Gizmos.DrawLine(transform.position, position);
        }
    }
}