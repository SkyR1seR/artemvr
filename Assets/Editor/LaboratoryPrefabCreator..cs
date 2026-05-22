#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using UnityEngine.XR.Interaction.Toolkit;

public class LaboratoryPrefabCreator : EditorWindow
{
    private const string PrefabRootFolder = "Assets/Prefabs";
    private const string LaboratoryFolder = "Assets/Prefabs/Laboratory";

    [MenuItem("Tools/Create Laboratory Prefabs")]
    static void CreateAllPrefabs()
    {
        // ��������, ��� ����� Laboratory ����������
        if (!AssetDatabase.IsValidFolder(LaboratoryFolder))
        {
            if (!AssetDatabase.IsValidFolder(PrefabRootFolder))
            {
                Debug.LogError($"����� {PrefabRootFolder} �� �������. �������� � ������� ��� �������� ����.");
                return;
            }
            AssetDatabase.CreateFolder(PrefabRootFolder, "Laboratory");
        }

        // ��������� ������� ����������� ����� (��������)
        bool hasSnapPoint = System.Type.GetType("SnapPoint") != null;
        bool hasRemovableWeight = System.Type.GetType("RemovableWeight") != null;
        if (!hasSnapPoint || !hasRemovableWeight)
        {
            Debug.LogWarning("�� ������� ������� SnapPoint ��� RemovableWeight. ���������, ��� ��� ���� � �������.");
        }

        // ������ �������
        CreateWeightPrefab();
        CreateLeverPrefab();

        AssetDatabase.Refresh();
        Debug.Log($"������� ������� � ����� {LaboratoryFolder}");
    }

    static void CreateWeightPrefab()
    {
        // ��������� ������
        GameObject weightObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        weightObj.name = "Weight";
        weightObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        // �������� (������� �������)
        Renderer renderer = weightObj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.red;
        renderer.material = mat;

        // ����������
        Rigidbody rb = weightObj.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.useGravity = true;

        // XR Grab Interactable (�� XR Interaction Toolkit)
        var grab = weightObj.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.trackPosition = true;
        grab.trackRotation = true;

        // ��������� RemovableWeight, ���� ����� ����������
        System.Type removableWeightType = System.Type.GetType("RemovableWeight");
        if (removableWeightType != null)
            weightObj.AddComponent(removableWeightType);
        else
            Debug.LogWarning("RemovableWeight �� ��������: ������ �� ������");

        // ��������� ������
        string localPath = $"{LaboratoryFolder}/Weight.prefab";
        PrefabUtility.SaveAsPrefabAsset(weightObj, localPath);
        DestroyImmediate(weightObj);
    }

    static void CreateLeverPrefab()
    {
        // �������� ������
        GameObject leverRoot = new GameObject("Lever");

        // --- ��������� ---
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.parent = leverRoot.transform;
        baseObj.transform.localPosition = Vector3.zero;
        baseObj.transform.localScale = new Vector3(0.6f, 0.2f, 0.6f);
        baseObj.isStatic = true;
        Material baseMat = new Material(Shader.Find("Standard"));
        baseMat.color = Color.gray;
        baseObj.GetComponent<Renderer>().material = baseMat;

        // --- ����� ---
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.parent = leverRoot.transform;
        arm.transform.localScale = new Vector3(2.0f, 0.1f, 0.2f);
        arm.transform.localPosition = new Vector3(0, 0.3f, 0);

        Material armMat = new Material(Shader.Find("Standard"));
        armMat.color = new Color(0.6f, 0.4f, 0.1f);
        arm.GetComponent<Renderer>().material = armMat;

        // Rigidbody
        Rigidbody rbArm = arm.AddComponent<Rigidbody>();
        rbArm.mass = 1f;
        rbArm.angularDrag  = 0.5f;
        rbArm.drag = 0.1f;

        // ������
        HingeJoint hinge = arm.AddComponent<HingeJoint>();
        hinge.connectedBody = baseObj.GetComponent<Rigidbody>();
        hinge.anchor = Vector3.zero;
        hinge.axis = Vector3.forward;
        JointLimits limits = hinge.limits;
        limits.min = -45f;
        limits.max = 45f;
        hinge.limits = limits;
        hinge.useLimits = true;

        // --- ����� ������� ---
        float[] distances = { 0.3f, 0.6f, 0.9f };
        Color leftColor = Color.blue;
        Color rightColor = Color.red;

        foreach (float dist in distances)
        {
            CreateSnapPoint(arm, new Vector3(-dist, 0, 0), $"LeftPoint_{dist}m", leftColor);
            CreateSnapPoint(arm, new Vector3(dist, 0, 0), $"RightPoint_{dist}m", rightColor);
        }

        // ��������� ������
        string localPath = $"{LaboratoryFolder}/Lever.prefab";
        PrefabUtility.SaveAsPrefabAsset(leverRoot, localPath);
        DestroyImmediate(leverRoot);
    }

    static void CreateSnapPoint(GameObject parent, Vector3 localPos, string name, Color color)
    {
        GameObject point = new GameObject(name);
        point.transform.parent = parent.transform;
        point.transform.localPosition = localPos;

        // ���������� ������ (�����)
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.parent = point.transform;
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * 0.08f;
        Material markerMat = new Material(Shader.Find("Standard"));
        markerMat.color = color;
        marker.GetComponent<Renderer>().material = markerMat;

        // Trigger-���������
        SphereCollider col = point.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.1f;

        // ��������� SnapPoint, ���� ����� ����������
        System.Type snapPointType = System.Type.GetType("SnapPoint");
        if (snapPointType != null)
        {
            var snap = point.AddComponent(snapPointType) as SnapPoint;
            if (snap != null)
                snap.snapPosition = point.transform;
        }
        else
        {
            Debug.LogWarning($"SnapPoint �� �������� ��� ����� {name}: ������ �� ������");
        }
    }
}
#endif