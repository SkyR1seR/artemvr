#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Инструмент редактора: полная настройка лабораторной работы №2
/// «Выяснение условий плавания тела в жидкости».
/// Запуск: Tools → Setup Lab2 Buoyancy
///
/// Создаёт в активной сцене (lab2):
///   - Стол-основание;
///   - Измерительный цилиндр (мензурку) с водой и поднимающимся уровнем;
///   - Пробирку-поплавок с пробкой (SteamVR-захват) + физика Архимеда;
///   - Кучу сухого песка и совок (досыпают песок → растёт масса);
///   - Весы с чашей (показания в граммах) и набор гирь;
///   - Проволочный крючок и сухую тряпку (приборы из списка);
///   - World-Space UI-панель: V₁, V₂, F_A, m, F_тяж, вывод;
///   - Контроллер BuoyancyExperiment.
/// При отсутствии SteamVR Player — добавляет Player и Teleporting.
///
/// Управление: SteamVR Interaction System (Interactable + Throwable).
/// </summary>
public class Lab2BuoyancySetup : EditorWindow
{
    // ─── Геометрия стола ─────────────────────────────────────────────────────
    private static readonly Vector3 TablePos = new Vector3(0f, 0f, 1.7f);
    private const float TableTopY    = 0.75f;
    private const float TableTopHalf = 0.03f;
    private static float SurfaceY => TableTopY + TableTopHalf;   // ≈ 0.78

    // ─── Позиции приборов ────────────────────────────────────────────────────
    private static readonly Vector3 CylinderPos = new Vector3(-0.45f, 0f, 1.7f);
    private static readonly Vector3 BalancePos  = new Vector3( 0.50f, 0f, 1.7f);
    private static readonly Vector3 SandPos     = new Vector3( 0.05f, 0f, 1.55f);
    private static readonly Vector3 TubeStart   = new Vector3(-0.18f, 0f, 1.60f);

    // ─── Параметры пробирки / воды ───────────────────────────────────────────
    private const float TubeRadius   = 0.0125f;
    private const float TubeLength    = 0.12f;
    private const float CylInnerR    = 0.03f;
    private const float WaterBaseVol = 4.0e-4f;   // 400 мл

    // ─── Гири ────────────────────────────────────────────────────────────────
    private static readonly (float massKg, string label)[] Weights =
    {
        (0.010f, "10 г"),
        (0.020f, "20 г"),
        (0.050f, "50 г"),
        (0.100f, "100 г"),
    };

    // ─── Entry point ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Lab2 Buoyancy")]
    public static void Run()
    {
        if (EditorUtility.DisplayDialog(
            "Настройка Lab2",
            "Добавить мензурку с водой, пробирку-поплавок, весы, песок и UI в текущую сцену?\n" +
            "Старые объекты 'Lab2_*' будут пересозданы.",
            "Да", "Отмена"))
        {
            SetupScene();
        }
    }

    private static void SetupScene()
    {
        foreach (var n in new[] {
            "Lab2_Table", "Lab2_Cylinder", "Lab2_TestTube", "Lab2_Balance",
            "Lab2_Weights", "Lab2_Sand", "Lab2_Props", "Lab2_UI", "Lab2_Experiment" })
            DestroyExisting(n);

        EnsurePlayer();

        BuildTable();
        var water   = BuildMeasuringCylinder();
        var tube    = BuildTestTube(water);
        var balance = BuildBalance();
        BuildWeights();
        BuildSand();
        BuildProps();
        var ui      = BuildUIPanel();
        BuildExperiment(water, tube, balance, ui);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Lab2Setup] Сцена настроена. Сохраните сцену (Ctrl+S).");
        EditorUtility.DisplayDialog("Готово",
            "Лабораторная работа №2 настроена.\n" +
            "• Опустите пробирку в мензурку — она плавает, уровень воды поднимается (V₂).\n" +
            "• Совком досыпайте песок из кучи в пробирку — масса растёт.\n" +
            "• Взвешивайте пробирку на чаше весов (показания в граммах).\n" +
            "Сохраните сцену: File → Save или Ctrl+S.", "OK");
    }

    // ─── Стол ────────────────────────────────────────────────────────────────
    private static void BuildTable()
    {
        var root = new GameObject("Lab2_Table");
        root.transform.position = TablePos;

        var top = CreateCube("TableTop", root.transform,
            new Vector3(0f, TableTopY, 0f),
            new Vector3(1.9f, TableTopHalf * 2f, 0.9f),
            new Color(0.62f, 0.48f, 0.34f));
        top.isStatic = true;

        float[] legX = { -0.85f, 0.85f, -0.85f, 0.85f };
        float[] legZ = { -0.38f, -0.38f, 0.38f, 0.38f };
        for (int i = 0; i < 4; i++)
        {
            var leg = CreateCylinder($"Leg{i}", root.transform,
                new Vector3(legX[i], TableTopY * 0.5f, legZ[i]),
                new Vector3(0.05f, TableTopY * 0.5f, 0.05f),
                new Color(0.45f, 0.34f, 0.22f));
            leg.isStatic = true;
        }
    }

    // ─── Измерительный цилиндр (мензурка) с водой ────────────────────────────
    private static WaterColumn BuildMeasuringCylinder()
    {
        var root = new GameObject("Lab2_Cylinder");
        root.transform.position = new Vector3(CylinderPos.x, SurfaceY, CylinderPos.z);

        // Дно (твёрдое — на него ложится утонувшая пробирка)
        var floor = CreateCylinder("Floor", root.transform,
            new Vector3(0f, 0.006f, 0f),
            new Vector3(CylInnerR * 2f, 0.006f, CylInnerR * 2f),
            new Color(0.7f, 0.7f, 0.75f));

        // Стекло мензурки (прозрачное, без коллайдера — пробирка входит внутрь)
        var glass = CreateCylinder("Glass", root.transform,
            new Vector3(0f, 0.11f, 0f),
            new Vector3(CylInnerR * 2f + 0.008f, 0.11f, CylInnerR * 2f + 0.008f),
            new Color(0.75f, 0.85f, 0.95f));
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        MakeTransparent(glass, new Color(0.75f, 0.85f, 0.95f, 0.18f));

        // Вода (прозрачно-синяя, управляется WaterColumn)
        var waterGO = CreateCylinder("Water", root.transform,
            new Vector3(0f, 0.08f, 0f),
            new Vector3(CylInnerR * 2f * 0.97f, 0.07f, CylInnerR * 2f * 0.97f),
            new Color(0.2f, 0.5f, 0.9f));
        Object.DestroyImmediate(waterGO.GetComponent<Collider>());
        MakeTransparent(waterGO, new Color(0.2f, 0.5f, 0.9f, 0.55f));

        var water = root.AddComponent<WaterColumn>();
        water.innerRadius = CylInnerR;
        water.bottomY     = SurfaceY + 0.012f;     // верх дна
        water.baseVolume  = WaterBaseVol;
        water.waterMesh   = waterGO.transform;

        // Подпись прибора
        CreateWorldLabel("Метка", root.transform,
            new Vector3(0f, 0.27f, 0f), "мензурка", 4, Color.black);

        return water;
    }

    // ─── Пробирка-поплавок с пробкой ─────────────────────────────────────────
    private static TestTubeFloat BuildTestTube(WaterColumn water)
    {
        var root = new GameObject("Lab2_TestTube");
        root.transform.position = new Vector3(TubeStart.x, SurfaceY + TubeLength * 0.5f + 0.001f, TubeStart.z);

        // Коллайдер-капсула (для физики, взвешивания и захвата)
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius    = TubeRadius;
        capsule.height    = TubeLength;
        capsule.direction = 1;   // ось Y

        var rb = root.AddComponent<Rigidbody>();
        rb.mass        = 0.018f;
        rb.useGravity  = true;
        rb.drag        = 0.2f;
        rb.angularDrag = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;   // стоит вертикально

        // Стекло пробирки (прозрачное)
        var body = CreateCapsule("Body", root.transform,
            Vector3.zero,
            new Vector3(TubeRadius * 2f, TubeLength * 0.5f, TubeRadius * 2f),
            new Color(0.8f, 0.9f, 1f));
        Object.DestroyImmediate(body.GetComponent<Collider>());
        MakeTransparent(body, new Color(0.8f, 0.9f, 1f, 0.25f));

        // Песок внутри (управляется TestTubeFloat)
        var sand = CreateCylinder("SandFill", root.transform,
            new Vector3(0f, -TubeLength * 0.5f + 0.01f, 0f),
            new Vector3(TubeRadius * 2f * 0.85f, 0.01f, TubeRadius * 2f * 0.85f),
            new Color(0.85f, 0.72f, 0.45f));
        Object.DestroyImmediate(sand.GetComponent<Collider>());

        // Пробка
        var cork = CreateCylinder("Cork", root.transform,
            new Vector3(0f, TubeLength * 0.5f + 0.006f, 0f),
            new Vector3(TubeRadius * 2f + 0.003f, 0.012f, TubeRadius * 2f + 0.003f),
            new Color(0.55f, 0.35f, 0.18f));
        Object.DestroyImmediate(cork.GetComponent<Collider>());

        // Метка массы
        var label = CreateWorldLabel("MassLabel", root.transform,
            new Vector3(0f, TubeLength * 0.5f + 0.06f, 0f), "18 г", 4, Color.white);

        // Захват (Interactable до пользовательских компонентов — без дубликата)
        root.AddComponent<Interactable>();

        var buoy = root.AddComponent<Buoyancy>();
        buoy.water      = water;
        buoy.bodyRadius = TubeRadius;
        buoy.bodyLength = TubeLength;

        var tube = root.AddComponent<TestTubeFloat>();
        tube.emptyMassKg = 0.018f;
        tube.maxSandMassKg = 0.060f;
        tube.bodyRadius = TubeRadius;
        tube.bodyLength = TubeLength;
        tube.sandFill   = sand.transform;
        tube.massLabel  = label;

        AddThrowable(root);
        return tube;
    }

    // ─── Весы с чашей ────────────────────────────────────────────────────────
    private static PanScale BuildBalance()
    {
        var root = new GameObject("Lab2_Balance");
        root.transform.position = new Vector3(BalancePos.x, SurfaceY, BalancePos.z);

        Color metal = new Color(0.55f, 0.55f, 0.6f);

        var baseDisk = CreateCylinder("Base", root.transform,
            new Vector3(0f, 0.02f, 0f), new Vector3(0.22f, 0.02f, 0.22f),
            new Color(0.3f, 0.3f, 0.32f));
        baseDisk.isStatic = true;

        var post = CreateCylinder("Post", root.transform,
            new Vector3(0f, 0.1f, 0f), new Vector3(0.03f, 0.1f, 0.03f), metal);
        post.isStatic = true;

        // Чаша (твёрдая — на неё кладут тело)
        var pan = CreateCylinder("Pan", root.transform,
            new Vector3(0f, 0.2f, 0f), new Vector3(0.18f, 0.01f, 0.18f), metal);
        pan.isStatic = true;

        // Триггер над чашей — считает массу
        var trig = new GameObject("PanTrigger");
        trig.transform.SetParent(root.transform, false);
        trig.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        var bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(0.16f, 0.12f, 0.16f);

        // Табло массы
        var display = CreateWorldLabel("Display", root.transform,
            new Vector3(0f, 0.36f, 0f), "0 г", 5, Color.green);

        var scale = trig.AddComponent<PanScale>();
        scale.display = display;

        CreateWorldLabel("Метка", root.transform,
            new Vector3(0f, 0.42f, 0f), "весы", 4, Color.black);

        return scale;
    }

    // ─── Гири ────────────────────────────────────────────────────────────────
    private static void BuildWeights()
    {
        var root = new GameObject("Lab2_Weights");
        float x0 = BalancePos.x + 0.18f;
        for (int i = 0; i < Weights.Length; i++)
        {
            var (massKg, label) = Weights[i];
            float size = Mathf.Lerp(0.02f, 0.04f, Mathf.InverseLerp(0.01f, 0.1f, massKg));
            Vector3 pos = new Vector3(x0 + i * 0.06f, SurfaceY + size * 0.5f + 0.002f, BalancePos.z + 0.12f);

            var w = CreateCylinder($"Ghyrya_{label.Replace(" ", "")}", root.transform,
                Vector3.zero, new Vector3(size, size * 0.5f, size),
                new Color(0.4f, 0.4f, 0.45f));
            w.transform.position = pos;

            var rb = w.AddComponent<Rigidbody>();
            rb.mass = massKg;

            // Метка под корнем секции (масштаб 1), чтобы текст не искажался.
            CreateWorldLabel($"Label_{i}", root.transform,
                pos + new Vector3(0f, size + 0.03f, 0f), label, 4, Color.white);

            AddThrowable(w);
        }
    }

    // ─── Куча песка и совок ──────────────────────────────────────────────────
    private static void BuildSand()
    {
        var root = new GameObject("Lab2_Sand");
        root.transform.position = new Vector3(SandPos.x, SurfaceY, SandPos.z);

        // Куча
        var pile = CreateSphere("Pile", root.transform, new Vector3(0f, 0.03f, 0f), 0.16f,
            new Color(0.85f, 0.72f, 0.45f));
        pile.transform.localScale = new Vector3(0.16f, 0.06f, 0.16f);
        pile.AddComponent<SandPile>();

        // Совок: чаша + ручка
        var scoop = new GameObject("Scoop");
        scoop.transform.position = new Vector3(SandPos.x + 0.22f, SurfaceY + 0.05f, SandPos.z);
        scoop.transform.SetParent(root.transform, true);   // под Lab2_Sand (для пересоздания)

        var cup = CreateSphere("Cup", scoop.transform, Vector3.zero, 0.05f,
            new Color(0.7f, 0.7f, 0.72f));
        cup.transform.localScale = new Vector3(0.05f, 0.03f, 0.05f);
        // твёрдый коллайдер чаши — чтобы совок лежал на столе и брался рукой
        // (примитив-сфера уже имеет SphereCollider)

        var handle = CreateCylinder("Handle", scoop.transform,
            new Vector3(0f, 0.02f, -0.06f),
            new Vector3(0.012f, 0.05f, 0.012f),
            new Color(0.4f, 0.28f, 0.16f));
        handle.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        // Песок в совке (визуал)
        var sandVis = CreateSphere("SandInScoop", scoop.transform, new Vector3(0f, 0.012f, 0f), 0.04f,
            new Color(0.85f, 0.72f, 0.45f));
        sandVis.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
        Object.DestroyImmediate(sandVis.GetComponent<Collider>());

        var rb = scoop.AddComponent<Rigidbody>();
        rb.mass = 0.05f;

        AddThrowable(scoop);

        // Кончик-триггер с логикой набора/высыпания песка
        var tip = new GameObject("ScoopTip");
        tip.transform.SetParent(scoop.transform, false);
        tip.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        var tc = tip.AddComponent<SphereCollider>();
        tc.isTrigger = true;
        tc.radius    = 0.05f;
        var ss = tip.AddComponent<SandScoop>();
        ss.sandPerScoop = 0.006f;
        ss.sandInScoop  = sandVis.GetComponent<Renderer>();

        CreateWorldLabel("Метка", root.transform, new Vector3(0f, 0.14f, 0f),
            "сухой песок", 4, Color.black);
    }

    // ─── Проволочный крючок и тряпка ─────────────────────────────────────────
    private static void BuildProps()
    {
        var root = new GameObject("Lab2_Props");

        // Проволочный крючок
        var hook = new GameObject("WireHook");
        hook.transform.position = new Vector3(-0.1f, SurfaceY + 0.06f, 1.95f);
        var wire = CreateCylinder("Wire", hook.transform,
            new Vector3(0f, 0.05f, 0f), new Vector3(0.006f, 0.06f, 0.006f),
            new Color(0.7f, 0.7f, 0.75f));
        var barb = CreateSphere("Barb", hook.transform, new Vector3(0f, -0.01f, 0.01f), 0.02f,
            new Color(0.7f, 0.7f, 0.75f));
        Object.DestroyImmediate(barb.GetComponent<Collider>());
        hook.transform.SetParent(root.transform, true);
        var hrb = hook.AddComponent<Rigidbody>();
        hrb.mass = 0.02f;
        CreateWorldLabel("Label", hook.transform, new Vector3(0f, 0.13f, 0f), "крючок", 4, Color.white);
        AddThrowable(hook);

        // Сухая тряпка
        Vector3 clothPos = new Vector3(0.15f, SurfaceY + 0.02f, 1.95f);
        var cloth = CreateCube("Cloth", root.transform, clothPos,
            new Vector3(0.12f, 0.02f, 0.1f),
            new Color(0.85f, 0.85f, 0.7f));
        var crb = cloth.AddComponent<Rigidbody>();
        crb.mass = 0.03f;
        CreateWorldLabel("ClothLabel", root.transform,
            clothPos + new Vector3(0f, 0.07f, 0f), "тряпка", 4, Color.black);
        AddThrowable(cloth);
    }

    // ─── UI-панель ───────────────────────────────────────────────────────────
    private static GameObject BuildUIPanel()
    {
        var root = new GameObject("Lab2_UI");
        root.transform.position = new Vector3(-1.55f, 1.45f, 1.7f);
        root.transform.rotation = Quaternion.Euler(0f, -22f, 0f);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        root.AddComponent<UnityEngine.UI.CanvasScaler>();
        root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(340f, 480f);
        root.transform.localScale = Vector3.one * 0.004f;

        CreateUIImage("Background", root.transform, Vector2.zero,
            new Vector2(340f, 480f), new Color(0.08f, 0.10f, 0.14f, 0.92f));

        CreateUIText("Title", root.transform, new Vector2(0f, 210f), new Vector2(320f, 50f),
            "Лабораторная работа №2\nУсловия плавания тела", 15, Color.white, bold: true);

        CreateUIImage("Divider", root.transform, new Vector2(0f, 176f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("V1",    root.transform, new Vector2(0f, 150f), new Vector2(320f, 28f),
            "V₁ = — мл", 14, new Color(0.7f, 0.85f, 1f));
        CreateUIText("V2",    root.transform, new Vector2(0f, 122f), new Vector2(320f, 28f),
            "V₂ = — мл", 14, new Color(0.7f, 0.85f, 1f));
        CreateUIText("Buoy",  root.transform, new Vector2(0f, 94f),  new Vector2(320f, 28f),
            "F_A = — Н", 14, new Color(0.7f, 0.85f, 1f));

        CreateUIImage("Divider2", root.transform, new Vector2(0f, 72f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Mass",    root.transform, new Vector2(0f, 48f), new Vector2(320f, 28f),
            "m = — г", 14, new Color(0.9f, 0.95f, 0.8f));
        CreateUIText("Weight",  root.transform, new Vector2(0f, 20f), new Vector2(320f, 28f),
            "F_тяж = — Н", 14, new Color(0.9f, 0.95f, 0.8f));
        CreateUIText("Balance", root.transform, new Vector2(0f, -8f), new Vector2(320f, 28f),
            "Весы: — г", 14, new Color(0.85f, 0.85f, 0.85f));

        CreateUIImage("Divider3", root.transform, new Vector2(0f, -32f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Verdict", root.transform, new Vector2(0f, -90f), new Vector2(320f, 90f),
            "Опустите пробирку\nв воду", 16, Color.green, bold: true);

        CreateUIText("Hint", root.transform, new Vector2(0f, -178f), new Vector2(320f, 80f),
            "Досыпайте песок совком, пока пробирка не утонет.\nF_A = ρ·g·(V₂−V₁),  F_тяж = m·g.", 10,
            new Color(0.95f, 0.9f, 0.6f));

        return root;
    }

    // ─── Контроллер ──────────────────────────────────────────────────────────
    private static void BuildExperiment(WaterColumn water, TestTubeFloat tube,
        PanScale balance, GameObject ui)
    {
        var go = new GameObject("Lab2_Experiment");
        var exp = go.AddComponent<BuoyancyExperiment>();
        exp.water   = water;
        exp.tube    = tube;
        exp.balance = balance;

        exp.v1Text      = FindUIText(ui, "V1");
        exp.v2Text      = FindUIText(ui, "V2");
        exp.buoyText    = FindUIText(ui, "Buoy");
        exp.massText    = FindUIText(ui, "Mass");
        exp.weightText  = FindUIText(ui, "Weight");
        exp.balanceText = FindUIText(ui, "Balance");
        exp.verdictText = FindUIText(ui, "Verdict");
        exp.hintText    = FindUIText(ui, "Hint");
    }

    // ─── SteamVR Player ──────────────────────────────────────────────────────
    private static void EnsurePlayer()
    {
        if (Object.FindObjectOfType<Player>() != null) return;

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/SteamVR/InteractionSystem/Core/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0f, 0.4f);
            Debug.Log("[Lab2Setup] Добавлен SteamVR Player.");
        }
        else
        {
            Debug.LogWarning("[Lab2Setup] Player.prefab не найден — добавьте SteamVR Player вручную.");
        }

        if (Object.FindObjectOfType<Teleport>() == null)
        {
            var teleportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SteamVR/InteractionSystem/Teleport/Prefabs/Teleporting.prefab");
            if (teleportPrefab != null)
                PrefabUtility.InstantiatePrefab(teleportPrefab);
        }
    }

    // ─── Вспомогательное ─────────────────────────────────────────────────────
    private static void AddThrowable(GameObject go)
    {
        var t = go.AddComponent<Throwable>();
        t.attachmentFlags =
            Hand.AttachmentFlags.ParentToHand |
            Hand.AttachmentFlags.DetachFromOtherHand |
            Hand.AttachmentFlags.TurnOnKinematic;
        t.restoreOriginalParent = false;
    }

    private static GameObject CreateCube(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
        => CreatePrimitive(PrimitiveType.Cube, name, parent, localPos, localScale, color);

    private static GameObject CreateCylinder(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
        => CreatePrimitive(PrimitiveType.Cylinder, name, parent, localPos, localScale, color);

    private static GameObject CreateCapsule(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
        => CreatePrimitive(PrimitiveType.Capsule, name, parent, localPos, localScale, color);

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
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
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * worldScale;
        SetColor(go, color);
        return go;
    }

    private static TextMeshPro CreateWorldLabel(string name, Transform parent,
        Vector3 localPos, string text, float fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * 0.4f;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        return tmp;
    }

    private static void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(r.sharedMaterial) { color = color };
        r.sharedMaterial = mat;
    }

    private static void MakeTransparent(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var sh = Shader.Find("Standard");
        var m  = sh != null ? new Material(sh) : new Material(r.sharedMaterial);
        m.color = color;
        if (sh != null)
        {
            m.SetFloat("_Mode", 3f);   // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
        r.sharedMaterial = m;
    }

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
            Debug.Log($"[Lab2Setup] Удалён старый объект '{name}'.");
        }
    }
}
#endif
