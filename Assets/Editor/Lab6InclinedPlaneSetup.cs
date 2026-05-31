#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Инструмент редактора: полная настройка лабораторной работы №6
/// «Определение КПД при подъёме тела по наклонной плоскости».
/// Запуск: Tools → Setup Lab6 Inclined Plane
///
/// Создаёт в активной сцене (lab6):
///   - Стол-основание;
///   - Штатив с муфтой и лапкой, удерживающей верхний конец доски;
///   - Доску (наклонную плоскость) под фиксированным углом;
///   - Пружинный динамометр (SteamVR-захват) с крючком и шкалой;
///   - Набор брусков с разными весами (100 / 200 / 500 г);
///   - Измерительную ленту вдоль доски;
///   - World-Space UI-панель с результатами (m, F, S, h, A, η);
///   - Контроллер InclinedPlaneEfficiency, связывающий приборы.
/// При отсутствии в сцене SteamVR Player — добавляет Player и Teleporting.
///
/// Управление: SteamVR Interaction System (Interactable + Throwable).
/// </summary>
public class Lab6InclinedPlaneSetup : EditorWindow
{
    // ─── Геометрия ───────────────────────────────────────────────────────────
    private static readonly Vector3 TablePos = new Vector3(0f, 0f, 1.8f);
    private const float TableTopY    = 0.75f;   // центр столешницы
    private const float TableTopHalf = 0.03f;   // половина толщины
    private static float SurfaceY => TableTopY + TableTopHalf;   // верх стола ≈ 0.78

    private const float InclineAngle = 30f;     // стартовый угол наклона доски, градусы
    private const float MinAngle      = 10f;    // минимальный угол (регулируется в VR)
    private const float MaxAngle      = 50f;    // максимальный угол
    private const float BoardLength   = 1.0f;   // длина доски, м
    private const float BoardWidth    = 0.30f;
    private const float BoardThick    = 0.04f;

    // Нижний край доски на столешнице
    private static readonly Vector3 BoardLowEnd = new Vector3(-0.55f, SurfaceY, 1.8f);

    // ─── Бруски ──────────────────────────────────────────────────────────────
    private static readonly (float massKg, Color color, string label)[] Blocks =
    {
        (0.100f, new Color(0.30f, 0.55f, 1.00f), "100 г"),
        (0.200f, new Color(0.25f, 0.75f, 0.35f), "200 г"),
        (0.500f, new Color(1.00f, 0.55f, 0.15f), "500 г"),
    };

    private static PhysicMaterial _frictionMat;

    // ─── Entry point ─────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Lab6 Inclined Plane")]
    public static void Run()
    {
        if (EditorUtility.DisplayDialog(
            "Настройка Lab6",
            "Добавить наклонную плоскость, штатив, динамометр, бруски и UI в текущую сцену?\n" +
            "Старые объекты 'Lab6_*' будут пересозданы.",
            "Да", "Отмена"))
        {
            SetupScene();
        }
    }

    private static void SetupScene()
    {
        DestroyExisting("Lab6_Table");
        DestroyExisting("Lab6_Tripod");
        DestroyExisting("Lab6_Board");
        DestroyExisting("Lab6_Dynamometer");
        DestroyExisting("Lab6_Blocks");
        DestroyExisting("Lab6_MeasuringTape");
        DestroyExisting("Lab6_UI");
        DestroyExisting("Lab6_Experiment");

        _frictionMat = new PhysicMaterial("Lab6_Friction")
        {
            dynamicFriction = 0.30f,
            staticFriction  = 0.40f,
            frictionCombine = PhysicMaterialCombine.Average,
            bounciness      = 0f
        };

        EnsurePlayer();

        var board      = BuildBoard();
        BuildTable();
        BuildTripod(board.transform);
        var tape       = BuildMeasuringTape(board.transform);
        var dyn        = BuildDynamometer();
        BuildBlocks();
        var ui         = BuildUIPanel();
        BuildExperiment(dyn, tape, board, ui);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[Lab6Setup] Сцена настроена. Сохраните сцену (Ctrl+S).");
        EditorUtility.DisplayDialog("Готово",
            "Лабораторная работа №6 настроена.\n" +
            "• Угол наклона доски меняйте красной ручкой-регулятором (CircularDrive).\n" +
            "• Возьмите динамометр, прицепите брусок крючком и тяните вверх по доске.\n" +
            "Сохраните сцену: File → Save или Ctrl+S.", "OK");
    }

    // ─── Стол ────────────────────────────────────────────────────────────────
    private static void BuildTable()
    {
        var root = new GameObject("Lab6_Table");
        root.transform.position = TablePos;

        var top = CreateCube("TableTop", root.transform,
            new Vector3(0f, TableTopY, 0f),
            new Vector3(1.8f, TableTopHalf * 2f, 0.9f),
            new Color(0.62f, 0.48f, 0.34f));
        top.isStatic = true;

        float[] legX = { -0.8f, 0.8f, -0.8f, 0.8f };
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

    // ─── Доска (наклонная плоскость, регулируемый угол) ──────────────────────
    // Корень — шарнир у нижнего края доски на столе. Доска поворачивается
    // вокруг него ручкой-регулятором (SteamVR CircularDrive). Угол ограничен
    // диапазоном [MinAngle, MaxAngle]; локальная ось вращения — Z.
    private static GameObject BuildBoard()
    {
        var root = new GameObject("Lab6_Board");
        root.transform.position = BoardLowEnd;          // шарнир у нижнего края
        root.transform.rotation = Quaternion.identity;  // угол задаст CircularDrive

        // Кинематический Rigidbody: коллайдеры доски двигаются корректно для физики.
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Доска смещена на половину длины вперёд — её нижний край совпадает с шарниром.
        float half = BoardLength * 0.5f;
        var plank = CreateCube("Plank", root.transform,
            new Vector3(half, 0f, 0f),
            new Vector3(BoardLength, BoardThick, BoardWidth),
            new Color(0.80f, 0.66f, 0.45f));
        ApplyFriction(plank);
        // Свой Interactable «поглощает» наведение руки, чтобы касание поверхности
        // не запускало поворот доски (поворот — только за ручку-регулятор).
        plank.AddComponent<Interactable>();

        // Невысокие бортики по краям, чтобы брусок не съезжал вбок.
        float railOffset = BoardWidth * 0.5f;
        foreach (float z in new[] { -railOffset, railOffset })
        {
            var rail = CreateCube($"Rail_{(z < 0 ? "L" : "R")}", root.transform,
                new Vector3(half, BoardThick * 0.5f + 0.012f, z),
                new Vector3(BoardLength, 0.024f, 0.012f),
                new Color(0.55f, 0.42f, 0.25f));
            ApplyFriction(rail);
        }

        // Ручка-регулятор: рычажок сбоку, обращённый к студенту (−Z).
        var knobCollider = BuildTiltKnob(root.transform, half);

        // CircularDrive: взять ручку и вращать вокруг оси Z доски.
        root.AddComponent<Interactable>();
        var drive = root.AddComponent<CircularDrive>();
        drive.axisOfRotation  = CircularDrive.Axis_t.ZAxis;
        drive.limited         = true;
        drive.minAngle        = MinAngle;
        drive.maxAngle        = MaxAngle;
        drive.forceStart      = true;
        drive.startAngle      = InclineAngle;
        drive.rotateGameObject = true;
        drive.hoverLock       = true;          // держит вращение, пока зажата кнопка
        drive.childCollider   = knobCollider;

        return root;
    }

    // Рычажок-регулятор наклона. Возвращает коллайдер для захвата.
    private static Collider BuildTiltKnob(Transform boardRoot, float half)
    {
        var knob = new GameObject("TiltKnob");
        knob.transform.SetParent(boardRoot, false);
        knob.transform.localPosition = new Vector3(half * 1.2f, 0f, -(BoardWidth * 0.5f + 0.02f));

        // Стержень рычажка (вдоль −Z, к студенту)
        var stem = CreateCube("Stem", knob.transform,
            new Vector3(0f, 0f, -0.06f),
            new Vector3(0.02f, 0.02f, 0.12f),
            new Color(0.30f, 0.30f, 0.32f));
        Object.DestroyImmediate(stem.GetComponent<Collider>());

        // Шар-рукоятка на конце
        var grip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grip.name = "Grip";
        grip.transform.SetParent(knob.transform, false);
        grip.transform.localPosition = new Vector3(0f, 0f, -0.13f);
        grip.transform.localScale    = Vector3.one * 0.06f;
        SetColor(grip, new Color(0.90f, 0.30f, 0.20f));

        return grip.GetComponent<Collider>();
    }

    // ─── Штатив с муфтой и лапкой (следует за наклоном доски) ────────────────
    private static void BuildTripod(Transform boardPivot)
    {
        var root = new GameObject("Lab6_Tripod");
        Vector3 standPos = new Vector3(0.72f, 0f, 1.8f);
        root.transform.position = standPos;

        Color metal = new Color(0.55f, 0.55f, 0.58f);

        // Основание-тренога (тяжёлый плоский диск)
        var baseDisk = CreateCylinder("Base", root.transform,
            new Vector3(0f, 0.02f, 0f),
            new Vector3(0.35f, 0.02f, 0.35f),
            new Color(0.30f, 0.30f, 0.32f));
        baseDisk.isStatic = true;

        // Вертикальный стержень (достаёт до максимального угла)
        float rodTop = 1.7f;
        var rod = CreateCylinder("Rod", root.transform,
            new Vector3(0f, rodTop * 0.5f, 0f),
            new Vector3(0.035f, rodTop * 0.5f, 0.035f),
            metal);
        rod.isStatic = true;

        // Муфта — скользит по стержню за верхним концом доски
        var coupling = CreateCube("Coupling", root.transform,
            new Vector3(0f, 1.0f, 0f),
            new Vector3(0.08f, 0.08f, 0.08f),
            new Color(0.20f, 0.20f, 0.22f));

        // Лапка — держатель от муфты к доске
        var clampArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        clampArm.name = "ClampArm";
        clampArm.transform.SetParent(root.transform, false);
        clampArm.transform.localScale = new Vector3(0.025f, 0.15f, 0.025f);
        SetColor(clampArm, metal);

        // Головка лапки, удерживающая край доски
        var clampHead = CreateCube("ClampHead", root.transform,
            Vector3.zero,
            new Vector3(0.05f, 0.10f, 0.14f),
            new Color(0.20f, 0.20f, 0.22f));

        // Следящий компонент: держит муфту/лапку на верхнем конце доски
        var follower = root.AddComponent<InclineClampFollower>();
        follower.boardPivot  = boardPivot;
        follower.boardLength = BoardLength;
        follower.rod         = rod.transform;
        follower.coupling    = coupling.transform;
        follower.clampArm    = clampArm.transform;
        follower.clampHead   = clampHead.transform;
    }

    // ─── Измерительная лента (закреплена на доске, наклоняется вместе с ней) ──
    private static MeasuringTape BuildMeasuringTape(Transform boardPivot)
    {
        var root = new GameObject("Lab6_MeasuringTape");
        // Дочерний объект доски: лента лежит вдоль плоскости и тилтуется с ней.
        root.transform.SetParent(boardPivot, false);
        float half = BoardLength * 0.5f;
        root.transform.localPosition = new Vector3(
            half, BoardThick * 0.5f + 0.01f, BoardWidth * 0.5f + 0.06f);
        root.transform.localRotation = Quaternion.identity;

        // Полоса ленты вдоль доски
        var strip = CreateCube("Strip", root.transform,
            Vector3.zero,
            new Vector3(BoardLength, 0.004f, 0.04f),
            new Color(0.95f, 0.92f, 0.30f));
        strip.GetComponent<Collider>().enabled = false;

        // Табло расстояния над лентой
        var labelGO = new GameObject("DistanceLabel");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0.12f, 0.05f);
        labelGO.transform.localScale    = Vector3.one * 0.4f;
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = "S = 0.00 м";
        tmp.fontSize  = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.black;

        var tape = root.AddComponent<MeasuringTape>();
        tape.distanceLabel = tmp;
        return tape;
    }

    // ─── Динамометр ──────────────────────────────────────────────────────────
    private static SpringDynamometer BuildDynamometer()
    {
        var root = new GameObject("Lab6_Dynamometer");
        root.transform.position = new Vector3(-0.5f, SurfaceY + 0.18f, 2.05f);

        // Коллайдер захвата на самом интерактивном объекте
        var rootCol = root.AddComponent<BoxCollider>();
        rootCol.size   = new Vector3(0.06f, 0.34f, 0.03f);
        rootCol.center = Vector3.zero;

        var rb = root.AddComponent<Rigidbody>();
        rb.mass        = 0.1f;
        rb.drag        = 0.4f;
        rb.angularDrag = 1.0f;

        // Корпус-шкала
        var casing = CreateCube("Casing", root.transform,
            Vector3.zero,
            new Vector3(0.055f, 0.30f, 0.02f),
            new Color(0.85f, 0.85f, 0.88f));
        Object.DestroyImmediate(casing.GetComponent<Collider>());

        // Циферблат
        var dial = CreateCube("Dial", root.transform,
            new Vector3(0f, 0.07f, -0.012f),
            new Vector3(0.05f, 0.10f, 0.004f),
            Color.white);
        Object.DestroyImmediate(dial.GetComponent<Collider>());

        // Стрелка (поворачивается вокруг локальной оси Z)
        var needlePivot = new GameObject("NeedlePivot");
        needlePivot.transform.SetParent(root.transform, false);
        needlePivot.transform.localPosition = new Vector3(0f, 0.07f, -0.015f);
        var needleBar = CreateCube("NeedleBar", needlePivot.transform,
            new Vector3(0f, 0.025f, 0f),
            new Vector3(0.006f, 0.05f, 0.004f),
            new Color(0.85f, 0.10f, 0.10f));
        Object.DestroyImmediate(needleBar.GetComponent<Collider>());

        // Крючок снизу
        var hook = new GameObject("Hook");
        hook.transform.SetParent(root.transform, false);
        hook.transform.localPosition = new Vector3(0f, -0.17f, 0f);
        var hookCol = hook.AddComponent<SphereCollider>();
        hookCol.isTrigger = true;
        hookCol.radius    = 0.05f;
        var hookVis = CreateSphere("HookVis", hook.transform, Vector3.zero, 0.03f,
            new Color(0.7f, 0.7f, 0.2f));
        Object.DestroyImmediate(hookVis.GetComponent<Collider>());

        // Числовое табло (World Space Canvas)
        ForceMeter forceMeter = BuildDynamometerReadout(root.transform, out TextMeshProUGUI readout);

        // Компоненты
        var hookComp = hook.AddComponent<DynamometerHook>();

        root.AddComponent<Interactable>();
        var throwable = root.AddComponent<Throwable>();
        throwable.attachmentFlags =
            Hand.AttachmentFlags.ParentToHand |
            Hand.AttachmentFlags.DetachFromOtherHand |
            Hand.AttachmentFlags.TurnOnKinematic;
        throwable.restoreOriginalParent = false;

        var dyn = root.AddComponent<SpringDynamometer>();
        dyn.hook          = hook.transform;
        dyn.needle        = needlePivot.transform;
        dyn.forceMeter    = forceMeter;
        dyn.fullScaleForce = 10f;

        hookComp.dynamometer = dyn;

        return dyn;
    }

    private static ForceMeter BuildDynamometerReadout(Transform parent, out TextMeshProUGUI readout)
    {
        var canvasGO = new GameObject("Readout");
        canvasGO.transform.SetParent(parent, false);
        canvasGO.transform.localPosition = new Vector3(0f, -0.05f, -0.02f);
        canvasGO.transform.localScale    = Vector3.one * 0.0016f;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 40f);

        readout = CreateUIText("Value", canvasGO.transform, Vector2.zero,
            new Vector2(120f, 40f), "0.0 N", 22, Color.black, bold: true);

        var fm = parent.gameObject.AddComponent<ForceMeter>();
        fm.displayText = readout;
        return fm;
    }

    // ─── Бруски ──────────────────────────────────────────────────────────────
    private static void BuildBlocks()
    {
        var root = new GameObject("Lab6_Blocks");
        root.transform.position = Vector3.zero;

        float[] xs = { -0.15f, 0.15f, 0.45f };
        for (int i = 0; i < Blocks.Length; i++)
        {
            var (massKg, color, label) = Blocks[i];
            float size = Mathf.Lerp(0.07f, 0.12f, Mathf.InverseLerp(0.1f, 0.5f, massKg));
            Vector3 pos = new Vector3(xs[i], SurfaceY + size * 0.5f + 0.005f, 2.05f);
            CreateBlock($"Brusok_{label.Replace(" ", "")}", root.transform, pos, size, massKg, color, label);
        }
    }

    private static void CreateBlock(string name, Transform parent, Vector3 worldPos,
        float size, float massKg, Color color, string label)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position   = worldPos;
        go.transform.localScale = new Vector3(size, size, size);
        SetColor(go, color);
        ApplyFriction(go);

        var rb = go.AddComponent<Rigidbody>();
        rb.mass        = massKg;
        rb.drag        = 0.2f;
        rb.angularDrag = 0.5f;

        // Interactable добавляем до Brusok, чтобы RequireComponent не создал дубликат.
        go.AddComponent<Interactable>();

        // Brusok должен стоять до Throwable: при захвате он сначала снимается с крючка.
        var brusok = go.AddComponent<Brusok>();
        brusok.massKg = massKg;

        var throwable = go.AddComponent<Throwable>();
        throwable.attachmentFlags =
            Hand.AttachmentFlags.ParentToHand |
            Hand.AttachmentFlags.DetachFromOtherHand |
            Hand.AttachmentFlags.TurnOnKinematic;
        throwable.restoreOriginalParent = false;

        // Метка массы
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        labelGO.transform.localScale    = Vector3.one * (1f / size) * 0.05f;
        var tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = label;
        tmp.fontSize  = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        brusok.massLabel = tmp;
    }

    // ─── UI-панель ───────────────────────────────────────────────────────────
    private static GameObject BuildUIPanel()
    {
        var root = new GameObject("Lab6_UI");
        root.transform.position = new Vector3(-1.55f, 1.45f, 1.85f);
        root.transform.rotation = Quaternion.Euler(0f, -25f, 0f);

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

        CreateUIText("Title", root.transform, new Vector2(0f, 210f),
            new Vector2(320f, 50f),
            "Лабораторная работа №6\nКПД наклонной плоскости", 15, Color.white, bold: true);

        CreateUIImage("Divider", root.transform, new Vector2(0f, 176f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Angle",    root.transform, new Vector2(0f, 150f), new Vector2(320f, 30f),
            "Угол:   α = — °", 14, new Color(1f, 0.85f, 0.5f));
        CreateUIText("Mass",     root.transform, new Vector2(0f, 122f), new Vector2(320f, 30f),
            "Масса:  m = — кг", 14, new Color(0.85f, 0.9f, 1f));
        CreateUIText("Force",    root.transform, new Vector2(0f, 94f),  new Vector2(320f, 30f),
            "Сила:   F = — Н", 14, new Color(0.85f, 0.9f, 1f));
        CreateUIText("Distance", root.transform, new Vector2(0f, 66f),  new Vector2(320f, 30f),
            "Путь:   S = — м", 14, new Color(0.85f, 0.9f, 1f));
        CreateUIText("Height",   root.transform, new Vector2(0f, 38f),  new Vector2(320f, 30f),
            "Высота: h = — м", 14, new Color(0.85f, 0.9f, 1f));

        CreateUIImage("Divider2", root.transform, new Vector2(0f, 14f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Work", root.transform, new Vector2(0f, -28f), new Vector2(320f, 60f),
            "A_пол = — Дж\nA_зат = — Дж", 13, new Color(0.8f, 0.95f, 0.8f));

        CreateUIText("Efficiency", root.transform, new Vector2(0f, -90f), new Vector2(320f, 44f),
            "η = — %", 22, Color.green, bold: true);

        CreateUIImage("Divider3", root.transform, new Vector2(0f, -128f),
            new Vector2(310f, 2f), new Color(0.5f, 0.5f, 0.5f));

        CreateUIText("Hint", root.transform, new Vector2(0f, -172f), new Vector2(320f, 70f),
            "Возьмите динамометр и прицепите брусок крючком.\nУгол доски меняйте красной ручкой-регулятором.", 11,
            new Color(0.95f, 0.9f, 0.6f));

        return root;
    }

    // ─── Контроллер эксперимента ─────────────────────────────────────────────
    private static void BuildExperiment(SpringDynamometer dyn, MeasuringTape tape,
        GameObject board, GameObject ui)
    {
        var go = new GameObject("Lab6_Experiment");
        var exp = go.AddComponent<InclinedPlaneEfficiency>();

        exp.dynamometer   = dyn;
        exp.measuringTape = tape;
        exp.inclinedPlane = board.transform;

        exp.angleText      = FindUIText(ui, "Angle");
        exp.massText       = FindUIText(ui, "Mass");
        exp.forceText      = FindUIText(ui, "Force");
        exp.distanceText   = FindUIText(ui, "Distance");
        exp.heightText     = FindUIText(ui, "Height");
        exp.workText       = FindUIText(ui, "Work");
        exp.efficiencyText = FindUIText(ui, "Efficiency");
        exp.hintText       = FindUIText(ui, "Hint");
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
            Debug.Log("[Lab6Setup] Добавлен SteamVR Player.");
        }
        else
        {
            Debug.LogWarning("[Lab6Setup] Player.prefab не найден — добавьте SteamVR Player вручную.");
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
    private static void ApplyFriction(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null && _frictionMat != null)
            col.sharedMaterial = _frictionMat;
    }

    private static GameObject CreateCube(string name, Transform parent,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
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

    private static void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(r.sharedMaterial) { color = color };
        r.sharedMaterial = mat;
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
            Debug.Log($"[Lab6Setup] Удалён старый объект '{name}'.");
        }
    }
}
#endif
