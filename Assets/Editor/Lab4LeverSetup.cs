#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Инструмент редактора: полная настройка лабораторной работы №4 «Рычаг».
/// Запуск: Tools → Setup Lab4 Lever
///
/// Создаёт в сцене lab4:
///   - Рычаг на штативе с 6 слотами (3 слева, 3 справа)
///   - Набор грузов (100 г, 200 г, 500 г, 1000 г) × 2 штуки каждый
///   - World-space UI-панель с показателями моментов и равновесия
///
/// Физика: FixedJoint соединяет груз с плечом рычага, Unity Physics
/// автоматически учитывает момент силы на шарнире (HingeJoint).
/// </summary>
public class Lab4LeverSetup : EditorWindow
{
    // ─── Позиции в сцене ─────────────────────────────────────────────────────
    private static readonly Vector3 LeverRootPos   = new Vector3(0f, 0f, 2.0f);
    private static readonly Vector3 WeightTablePos = new Vector3(1.6f, 0f, 2.0f);

    // Рычаг: плечо поднято на 1.1 м от пола (высота шарнира в VR-стойке)
    // Base (0.6×0.2×0.6) стоит на подставке; Arm (2×0.1×0.2) — на 0.3 выше
    private const float LeverHeight    = 1.05f;   // высота основания рычага над полом
    private const float ArmLocalY     = 0.30f;   // localY плеча относительно корня рычага
    // Arm world Y ≈ LeverHeight + ArmLocalY = 1.35 м
    // Плечо рычага в мировых единицах (localScale.x = 2)
    // Слоты: localX = ±0.3, ±0.6, ±0.9 → worldX = ±0.6, ±1.2, ±1.8 м
    private static readonly float[] SlotDistances = { 0.6f, 1.2f, 1.8f };
    private static readonly float[] SlotLocalX    = { 0.3f, 0.6f, 0.9f };

    // ─── Грузы ───────────────────────────────────────────────────────────────
    private static readonly (float massKg, Color color, string label)[] WeightTypes =
    {
        (0.100f, new Color(0.30f, 0.55f, 1.00f), "100 г"),
        (0.200f, new Color(0.25f, 0.75f, 0.35f), "200 г"),
        (0.500f, new Color(1.00f, 0.80f, 0.10f), "500 г"),
        (1.000f, new Color(1.00f, 0.35f, 0.20f), "1 кг"),
    };
    private const int WeightCopies = 2;

    // ─── Entry point ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Lab4 Lever")]
    public static void Run()
    {
        if (EditorUtility.DisplayDialog(
            "Настройка Lab4",
            "Добавить рычаг, грузы и UI в текущую сцену?\n" +
            "Если объекты 'Lever_Lab4' и 'WeightTable_Lab4' уже существуют, они будут удалены.",
            "Да", "Отмена"))
        {
            SetupScene();
        }
    }

    // ─── Основная логика ─────────────────────────────────────────────────────
    private static void SetupScene()
    {
        // Чистим предыдущую установку, если есть
        DestroyExisting("Lever_Lab4");
        DestroyExisting("WeightTable_Lab4");
        DestroyExisting("LeverUI_Lab4");
        DestroyExisting("LeverExperiment_Lab4");

        // Отключаем старый LeverBuilder в сцене, чтобы не конфликтовал
        DisableLegacyLeverBuilder();

        // Создаём все части
        var leverRoot   = BuildLever();
        var weightTable = BuildWeightTable();
        var uiPanel     = BuildUIPanel();
        var experiment  = BuildExperimentController(leverRoot, uiPanel);

        // Связываем слоты с экспериментом
        WireSlots(leverRoot, experiment);

        // Помечаем сцену изменённой
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Lab4LeverSetup] Сцена настроена. Сохраните сцену (Ctrl+S).");
        EditorUtility.DisplayDialog("Готово",
            "Лабораторная работа №4 настроена.\n" +
            "Сохраните сцену: File → Save или Ctrl+S.", "OK");
    }

    // ─── Рычаг ───────────────────────────────────────────────────────────────
    private static GameObject BuildLever()
    {
        // Корень
        var root = new GameObject("Lever_Lab4");
        root.transform.position = LeverRootPos + Vector3.up * LeverHeight;

        // Штатив (вертикальный цилиндр от пола до основания)
        var stand = CreateCylinder("Stand",
            parent: root.transform,
            localPos: new Vector3(0f, -LeverHeight * 0.5f, 0f),
            localScale: new Vector3(0.05f, LeverHeight * 0.5f, 0.05f),
            color: new Color(0.5f, 0.5f, 0.5f));
        stand.isStatic = true;

        // Основание рычага (статическое)
        var baseObj = CreateCube("Base",
            parent: root.transform,
            localPos: Vector3.zero,
            localScale: new Vector3(0.6f, 0.2f, 0.6f),
            color: new Color(0.55f, 0.55f, 0.55f));
        baseObj.isStatic = true;

        // Плечо рычага (динамический Rigidbody + HingeJoint)
        var arm = CreateCube("Arm",
            parent: root.transform,
            localPos: new Vector3(0f, ArmLocalY, 0f),
            localScale: new Vector3(2f, 0.1f, 0.2f),
            color: new Color(0.70f, 0.60f, 0.45f));

        var armRb = arm.AddComponent<Rigidbody>();
        armRb.mass           = 0.2f;   // лёгкое плечо, чтобы грузы доминировали
        armRb.drag        = 0.3f;
        armRb.angularDrag = 1.5f;   // умеренное затухание для устойчивости

        var hinge = arm.AddComponent<HingeJoint>();
        hinge.anchor  = Vector3.zero;  // ось в центре плеча
        hinge.axis    = Vector3.forward;
        hinge.useLimits = true;
        var limits = hinge.limits;
        limits.min  = -40f;
        limits.max  =  40f;
        hinge.limits = limits;
        // connectedBody = null → шарнир крепится к мировому пространству (неподвижная точка)

        // Ось-маркер (маленькая серая сфера в центре плеча)
        var pivot = CreateSphere("PivotMarker",
            parent: arm.transform,
            localPos: Vector3.zero,
            worldScale: 0.06f,
            color: new Color(0.25f, 0.25f, 0.25f));
        pivot.GetComponent<Collider>().enabled = false;

        // Слоты
        AddSlots(arm, armRb);

        return root;
    }

    private static void AddSlots(GameObject arm, Rigidbody armRb)
    {
        var leftSlots  = new List<(string name, Vector3 localPos, float dist)>();
        var rightSlots = new List<(string name, Vector3 localPos, float dist)>();

        for (int i = 0; i < SlotLocalX.Length; i++)
        {
            leftSlots.Add(($"Slot_L{i + 1}", new Vector3(-SlotLocalX[i], 0f, 0f), SlotDistances[i]));
            rightSlots.Add(($"Slot_R{i + 1}", new Vector3( SlotLocalX[i], 0f, 0f), SlotDistances[i]));
        }

        foreach (var (name, localPos, dist) in leftSlots)
            CreateSlot(name, arm.transform, localPos, dist, armRb, Color.cyan);

        foreach (var (name, localPos, dist) in rightSlots)
            CreateSlot(name, arm.transform, localPos, dist, armRb, Color.cyan);
    }

    private static void CreateSlot(string name, Transform armTransform,
        Vector3 localPos, float dist, Rigidbody armRb, Color markerColor)
    {
        // Дочерний объект-слот живёт в локальном пространстве плеча.
        // Шкала компенсирует scale плеча (2, 0.1, 0.2), чтобы
        // мировой радиус триггера и маркера был единообразным.
        var slotGO = new GameObject(name);
        slotGO.transform.SetParent(armTransform, worldPositionStays: false);
        slotGO.transform.localPosition = localPos;
        slotGO.transform.localRotation = Quaternion.identity;
        // Обратная шкала плеча → worldScale ≈ (1, 1, 1)
        slotGO.transform.localScale = new Vector3(0.5f, 10f, 5f);

        // Trigger-коллайдер
        var sc = slotGO.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius    = 0.12f;

        // Визуальный маркер (кольцо = маленький тор из Sphere-меша)
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Marker";
        marker.transform.SetParent(slotGO.transform, worldPositionStays: false);
        marker.transform.localPosition = new Vector3(0f, 0.08f, 0f);  // чуть выше центра плеча
        marker.transform.localScale    = new Vector3(0.08f, 0.08f, 0.08f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        SetColor(marker, markerColor);

        // Метка расстояния
        var labelGO = new GameObject("DistLabel");
        labelGO.transform.SetParent(slotGO.transform, worldPositionStays: false);
        labelGO.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        labelGO.transform.localScale    = new Vector3(0.06f, 0.06f, 0.06f);
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = $"{dist:F2} м";
        tmp.fontSize  = 8;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        // Компонент слота
        var slot = slotGO.AddComponent<LeverSlot>();
        slot.distanceFromPivot = dist;
        slot.armRigidbody      = armRb;
    }

    // ─── Стол с грузами ──────────────────────────────────────────────────────
    private static GameObject BuildWeightTable()
    {
        var tableRoot = new GameObject("WeightTable_Lab4");
        tableRoot.transform.position = WeightTablePos;

        // Поверхность стола
        var top = CreateCube("TableTop", tableRoot.transform,
            new Vector3(0f, 0.85f, 0f),
            new Vector3(0.8f, 0.04f, 0.6f),
            new Color(0.65f, 0.50f, 0.35f));
        top.isStatic = true;

        // Ножки стола
        float[] legX = { -0.35f,  0.35f, -0.35f,  0.35f };
        float[] legZ = { -0.25f, -0.25f,  0.25f,  0.25f };
        for (int i = 0; i < 4; i++)
        {
            var leg = CreateCylinder($"Leg{i}", tableRoot.transform,
                new Vector3(legX[i], 0.43f, legZ[i]),
                new Vector3(0.04f, 0.43f, 0.04f),
                new Color(0.5f, 0.38f, 0.25f));
            leg.isStatic = true;
        }

        // Грузы
        SpawnWeightsOnTable(tableRoot.transform);

        return tableRoot;
    }

    private static void SpawnWeightsOnTable(Transform tableRoot)
    {
        // Таблица поверхности: y = 0.85 + 0.02 = 0.87 (top table surface)
        float surfaceY = 0.87f;
        float spacing  = 0.12f;
        int totalWeights = WeightTypes.Length * WeightCopies;
        float startX = -(totalWeights - 1) * spacing * 0.5f;

        int idx = 0;
        foreach (var (massKg, color, labelStr) in WeightTypes)
        {
            for (int c = 0; c < WeightCopies; c++, idx++)
            {
                float scale = Mathf.Lerp(0.07f, 0.14f, massKg);  // маленький → большой
                float x = startX + idx * spacing;
                float y = surfaceY + scale * 0.5f;

                var w = CreateWeightObject(
                    name:   $"Weight_{labelStr.Replace(" ", "")}_{c + 1}",
                    parent: tableRoot,
                    worldPos: tableRoot.position + new Vector3(x, y, 0f),
                    scale:  scale,
                    massKg: massKg,
                    color:  color,
                    label:  labelStr);
            }
        }
    }

    private static GameObject CreateWeightObject(string name, Transform parent,
        Vector3 worldPos, float scale, float massKg, Color color, string label)
    {
        // Корпус груза (цилиндр)
        var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        w.name = name;
        w.transform.SetParent(parent, worldPositionStays: true);
        w.transform.position = worldPos;
        w.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
        SetColor(w, color);

        // Rigidbody
        var rb = w.AddComponent<Rigidbody>();
        rb.mass = massKg;

        // LeverWeight должен быть ДО Throwable, чтобы OnAttachedToHand
        // сначала освободил слот, а потом Throwable сделал rb.isKinematic = true
        var lw = w.AddComponent<LeverWeight>();
        lw.massKg = massKg;

        // SteamVR Interaction System: Interactable + Throwable
        w.AddComponent<Interactable>();
        var throwable = w.AddComponent<Throwable>();
        throwable.attachmentFlags =
            Hand.AttachmentFlags.ParentToHand |
            Hand.AttachmentFlags.DetachFromOtherHand |
            Hand.AttachmentFlags.TurnOnKinematic;
        throwable.restoreOriginalParent = false;

        // Метка TextMeshPro (мировое пространство, над грузом)
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(w.transform, worldPositionStays: false);
        labelGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        labelGO.transform.localScale    = Vector3.one * (1f / scale) * 0.08f;
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = label;
        tmp.fontSize  = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        lw.massLabel  = tmp;

        return w;
    }

    // ─── UI-панель (World Space Canvas) ─────────────────────────────────────
    private static GameObject BuildUIPanel()
    {
        var panelRoot = new GameObject("LeverUI_Lab4");
        panelRoot.transform.position = LeverRootPos + new Vector3(-1.8f, 1.5f, 2.0f);
        panelRoot.transform.rotation = Quaternion.Euler(0f, 20f, 0f);

        var canvas = panelRoot.AddComponent<Canvas>();
        canvas.renderMode        = RenderMode.WorldSpace;
        canvas.worldCamera       = Camera.main;
        panelRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        panelRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var rt = panelRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 420f);
        panelRoot.transform.localScale = Vector3.one * 0.004f;

        // Фон
        var bg = CreateUIImage("Background", panelRoot.transform,
            Vector2.zero, new Vector2(320f, 420f), new Color(0.08f, 0.10f, 0.14f, 0.92f));

        // Заголовок
        CreateUIText("Title", panelRoot.transform, new Vector2(0f, 175f),
            new Vector2(300f, 36f), "Лабораторная работа №4\nЗакон рычага", 14,
            Color.white, bold: true);

        // Разделитель
        CreateUIImage("Divider", panelRoot.transform,
            new Vector2(0f, 148f), new Vector2(290f, 2f), new Color(0.5f, 0.5f, 0.5f));

        // Левый момент
        CreateUIText("LabelLeft", panelRoot.transform,
            new Vector2(-80f, 120f), new Vector2(130f, 28f),
            "Левое плечо:", 11, new Color(0.5f, 0.85f, 1f));

        CreateUIText("LeftMoment", panelRoot.transform,
            new Vector2(-80f, 90f), new Vector2(130f, 28f),
            "M₁ = 0.000 Н·м", 12, Color.white);

        CreateUIText("LeftWeights", panelRoot.transform,
            new Vector2(-80f, 35f), new Vector2(130f, 80f),
            "Левое плечо:\n  — грузов нет", 9, new Color(0.85f, 0.85f, 0.85f));

        // Правый момент
        CreateUIText("LabelRight", panelRoot.transform,
            new Vector2(80f, 120f), new Vector2(130f, 28f),
            "Правое плечо:", 11, new Color(1f, 0.85f, 0.5f));

        CreateUIText("RightMoment", panelRoot.transform,
            new Vector2(80f, 90f), new Vector2(130f, 28f),
            "M₂ = 0.000 Н·м", 12, Color.white);

        CreateUIText("RightWeights", panelRoot.transform,
            new Vector2(80f, 35f), new Vector2(130f, 80f),
            "Правое плечо:\n  — грузов нет", 9, new Color(0.85f, 0.85f, 0.85f));

        // Статус равновесия
        CreateUIImage("Divider2", panelRoot.transform,
            new Vector2(0f, -45f), new Vector2(290f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Balance", panelRoot.transform,
            new Vector2(0f, -70f), new Vector2(300f, 36f),
            "⚖  РАВНОВЕСИЕ", 14, Color.green, bold: true);

        // Формула
        CreateUIImage("Divider3", panelRoot.transform,
            new Vector2(0f, -110f), new Vector2(290f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Formula", panelRoot.transform,
            new Vector2(0f, -160f), new Vector2(300f, 90f),
            "F₁·l₁  =  F₂·l₂\n0  =  0", 10, new Color(0.75f, 0.95f, 0.75f));

        return panelRoot;
    }

    // ─── Контроллер эксперимента ─────────────────────────────────────────────
    private static LeverExperiment BuildExperimentController(
        GameObject leverRoot, GameObject uiPanel)
    {
        var expGO = new GameObject("LeverExperiment_Lab4");
        var exp = expGO.AddComponent<LeverExperiment>();

        // Привязываем UI тексты
        exp.leftMomentText    = FindUIText(uiPanel, "LeftMoment");
        exp.rightMomentText   = FindUIText(uiPanel, "RightMoment");
        exp.balanceStatusText = FindUIText(uiPanel, "Balance");
        exp.formulaText       = FindUIText(uiPanel, "Formula");
        exp.leftWeightsText   = FindUIText(uiPanel, "LeftWeights");
        exp.rightWeightsText  = FindUIText(uiPanel, "RightWeights");

        return exp;
    }

    private static void WireSlots(GameObject leverRoot, LeverExperiment exp)
    {
        // Находим плечо рычага
        var arm = leverRoot.transform.Find("Arm");
        if (arm == null) { Debug.LogError("[Lab4LeverSetup] Объект 'Arm' не найден в Lever_Lab4"); return; }

        exp.leftSlots.Clear();
        exp.rightSlots.Clear();

        foreach (Transform child in arm)
        {
            var slot = child.GetComponent<LeverSlot>();
            if (slot == null) continue;

            if (child.name.StartsWith("Slot_L"))
                exp.leftSlots.Add(slot);
            else if (child.name.StartsWith("Slot_R"))
                exp.rightSlots.Add(slot);
        }

        // Сортируем по дистанции (от оси к краю)
        exp.leftSlots.Sort((a, b) => a.distanceFromPivot.CompareTo(b.distanceFromPivot));
        exp.rightSlots.Sort((a, b) => a.distanceFromPivot.CompareTo(b.distanceFromPivot));

        Debug.Log($"[Lab4LeverSetup] Слотов: {exp.leftSlots.Count} слева, {exp.rightSlots.Count} справа.");
    }

    private static void DisableLegacyLeverBuilder()
    {
        var lb = GameObject.Find("LevelBuilder");
        if (lb == null) return;
        var comp = lb.GetComponent<MonoBehaviour>();
        if (comp != null)
        {
            comp.enabled = false;
            Debug.Log("[Lab4LeverSetup] Старый LevelBuilder отключён.");
        }
    }

    // ─── Вспомогательные фабричные методы ───────────────────────────────────
    private static GameObject CreateCube(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = localScale;
        SetColor(go, color);
        return go;
    }

    private static GameObject CreateCylinder(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = localScale;
        SetColor(go, color);
        return go;
    }

    private static GameObject CreateSphere(string name, Transform parent,
        Vector3 localPos, float worldScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * worldScale;
        SetColor(go, color);
        return go;
    }

    private static void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        // Клонируем дефолтный материал примитива — он уже использует нужный шейдер (URP/Standard)
        var mat = new Material(r.sharedMaterial) { color = color };
        r.sharedMaterial = mat;
    }

    // ─── UI helpers ──────────────────────────────────────────────────────────
    private static GameObject CreateUIImage(string name, Transform parent,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return go;
    }

    private static TextMeshProUGUI CreateUIText(string name, Transform parent,
        Vector2 anchoredPos, Vector2 sizeDelta, string text,
        int fontSize, Color color, bool bold = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return tmp;
    }

    private static TextMeshProUGUI FindUIText(GameObject uiRoot, string name)
    {
        var t = uiRoot.transform.Find(name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void DestroyExisting(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            Object.DestroyImmediate(go);
            Debug.Log($"[Lab4LeverSetup] Удалён старый объект '{name}'.");
        }
    }
}
#endif
