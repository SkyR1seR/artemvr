#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class LaboratoryInclinedPlaneCreator : EditorWindow
{
    private const string PrefabRootFolder = "Assets/Prefabs";
    private const string TargetFolder = "Assets/Prefabs/InclinedPlane";

    [MenuItem("Tools/Create Inclined Plane Prefabs")]
    static void CreateAllPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(TargetFolder))
        {
            if (!AssetDatabase.IsValidFolder(PrefabRootFolder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder(PrefabRootFolder, "InclinedPlane");
        }

        CreateInclinedPlanePrefab();
        CreateBlockPrefab();
        CreateForceMeterPrefab();
        CreateMeasuringTapePrefab();

        AssetDatabase.Refresh();
        Debug.Log($"��� ������� ������� � {TargetFolder}");
    }

    static void CreateInclinedPlanePrefab()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "InclinedPlane";
        plane.transform.localScale = new Vector3(2f, 0.1f, 1f);
        plane.transform.rotation = Quaternion.Euler(30f, 0f, 0f); // ���� 30�
        plane.transform.position = new Vector3(0, 0, 0);

        // ��������
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.7f, 0.7f, 0.5f);
        plane.GetComponent<Renderer>().material = mat;

        // ��������� ��������� (��� ����)
        // ������ �����������
        plane.isStatic = true;

        // ���������
        string path = $"{TargetFolder}/InclinedPlane.prefab";
        PrefabUtility.SaveAsPrefabAsset(plane, path);
        DestroyImmediate(plane);
    }

    static void CreateBlockPrefab()
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Block";
        block.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);

        // �������� (������)
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.6f, 0.4f, 0.1f);
        block.GetComponent<Renderer>().material = mat;

        // ������
        Rigidbody rb = block.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDrag  = 0.5f;
        rb.drag = 0.5f;

        // ��� ����������� ������� � VR
        var grab = block.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;

        // ������ ��� ������� � ������������
        block.AddComponent<ExperimentBlock>();

        // ���������
        string path = $"{TargetFolder}/Block.prefab";
        PrefabUtility.SaveAsPrefabAsset(block, path);
        DestroyImmediate(block);
    }

    static void CreateForceMeterPrefab()
    {
        // ������� ������ ����������� (������ + ���� + �����)
        GameObject meter = new GameObject("ForceMeter");
        meter.transform.localScale = Vector3.one;

        // ������
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.parent = meter.transform;
        body.transform.localPosition = new Vector3(0, 0, 0);
        body.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = Color.gray;
        body.GetComponent<Renderer>().material = bodyMat;

        // ����� (�����)
        GameObject canvasGO = new GameObject("Display");
        canvasGO.transform.parent = meter.transform;
        canvasGO.transform.localPosition = new Vector3(0, 0.4f, 0);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        var textGO = new GameObject("Text");
        textGO.transform.parent = canvasGO.transform;
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "0.0 N";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100);
        rect.anchoredPosition = Vector2.zero;

        // ������ �����������
        var fm = meter.AddComponent<ForceMeter>();
        fm.displayText = text;

        // ���������
        string path = $"{TargetFolder}/ForceMeter.prefab";
        PrefabUtility.SaveAsPrefabAsset(meter, path);
        DestroyImmediate(meter);
    }

    static void CreateMeasuringTapePrefab()
    {
        GameObject tape = new GameObject("MeasuringTape");
        // ������ ������� (������� ���)
        GameObject ruler = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ruler.name = "Ruler";
        ruler.transform.parent = tape.transform;
        ruler.transform.localScale = new Vector3(2f, 0.02f, 0.1f);
        ruler.transform.localPosition = new Vector3(0, 0, 0);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.white;
        ruler.GetComponent<Renderer>().material = mat;

        // ������� ������� (����� ����� �����, �� ��� �������� �������)

        tape.AddComponent<MeasuringTape>();
        string path = $"{TargetFolder}/MeasuringTape.prefab";
        PrefabUtility.SaveAsPrefabAsset(tape, path);
        DestroyImmediate(tape);
    }
}
#endif