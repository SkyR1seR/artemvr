using UnityEngine;

public class LeverBuilder : MonoBehaviour
{
    [Header("��������� ������")]
    public float totalLength = 2.0f;      // ����� ����� ������
    public float width = 0.1f;            // ������ (�������) ������
    public float height = 0.2f;           // ������ ������
    public Material leverMaterial;        // �������� ������

    [Header("��� ��������")]
    public float pivotYOffset = 0.3f;     // ������ ��� ��� ����������
    public float pivotXOffset = 0f;       // �������� ��� �� ������ (0 = �����)

    [Header("����� �������")]
    public int pointsCountPerSide = 3;               // ���-�� ����� � ������ �������
    public float startDistance = 0.3f;               // ���������� �� ��� �� ������ �����
    public float stepDistance = 0.3f;                // ��� ����� �������
    public GameObject pointMarkerPrefab;              // ������-������������ ����� (�����/�����)
    public Color leftPointsColor = Color.blue;
    public Color rightPointsColor = Color.red;

    [Header("��������� (�����)")]
    public float baseWidth = 0.5f;
    public float baseHeight = 0.2f;
    public float baseDepth = 0.5f;
    public Material baseMaterial;

    void Start()
    {
        BuildLever();
    }

    void BuildLever()
    {
        // 1. �������� ��������� (��������� �����)
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "LeverBase";
        baseObj.transform.position = new Vector3(0, 0, 0);
        baseObj.transform.localScale = new Vector3(baseWidth, baseHeight, baseDepth);
        baseObj.GetComponent<Renderer>().material = baseMaterial;
        baseObj.isStatic = true;
        // ������� ���������, ����� �� �� �����? ������� ��� �����.

        // 2. �������� ������ ������
        GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lever.name = "LeverArm";
        lever.transform.localScale = new Vector3(totalLength, height, width);
        // ������������� ���, ����� ��� �������� ���� � ������ �����
        // �� ��������� ����� ���� ��������� � ��� ��������.
        // ����� ��� ���� � pivotXOffset �� ������, ������� �����.
        float pivotLocalPos = pivotXOffset; // � ��������� ����������� ������ (0 - �����)
        lever.transform.position = new Vector3(pivotLocalPos, pivotYOffset, 0);
        lever.GetComponent<Renderer>().material = leverMaterial;

        // ��������� ������
        Rigidbody rb = lever.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.angularDrag  = 0.5f;
        rb.drag  = 0.1f;

        // 3. HINGE JOINT (��� ��������)
        HingeJoint hinge = lever.AddComponent<HingeJoint>();
        hinge.connectedBody = baseObj.GetComponent<Rigidbody>();
        // ����������� ����� � ��������� ����������� ������ (���, ��� �������� ���)
        hinge.anchor = new Vector3(-pivotLocalPos, 0, 0);
        // ������������: ���� pivotLocalPos = 0, �� anchor = (0,0,0) - ��� � ������.
        // ���� pivotLocalPos = 0.2, �� anchor = (-0.2,0,0) - ��� ������� ����� ������������ ������.
        hinge.axis = Vector3.forward; // �������� ������ ��� Z
        // ������������ ���� (�����������)
        JointLimits limits = hinge.limits;
        limits.min = -45f;
        limits.max = 45f;
        hinge.limits = limits;
        hinge.useLimits = true;

        // 4. ����� ������� (��������� ��� �������� �������)
        // ����� ������� (������������� ����������� X)
        for (int i = 0; i < pointsCountPerSide; i++)
        {
            float distance = startDistance + i * stepDistance;
            Vector3 localPos = new Vector3(-distance, 0, 0);
            CreateSnapPoint(lever, localPos, $"LeftPoint_{distance}m", leftPointsColor);
        }
        // ������ �������
        for (int i = 0; i < pointsCountPerSide; i++)
        {
            float distance = startDistance + i * stepDistance;
            Vector3 localPos = new Vector3(distance, 0, 0);
            CreateSnapPoint(lever, localPos, $"RightPoint_{distance}m", rightPointsColor);
        }
    }

    void CreateSnapPoint(GameObject parent, Vector3 localPosition, string name, Color color)
    {
        GameObject point = new GameObject(name);
        point.transform.parent = parent.transform;
        point.transform.localPosition = localPosition;

        // ��������� ���������� ������ (���� ������ �� �����, ������ �����)
        GameObject marker;
        if (pointMarkerPrefab != null)
            marker = Instantiate(pointMarkerPrefab, point.transform);
        else
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.localScale = Vector3.one * 0.08f;
            marker.transform.SetParent(point.transform);
            marker.transform.localPosition = Vector3.zero;
        }
        marker.GetComponent<Renderer>().material.color = color;

        // ��������� Trigger-��������� ��� ����������� ������
        SphereCollider col = point.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.1f;

        // ��������� ������ SnapPoint (����� �������� ��������, ��. ����)
        SnapPoint snap = point.AddComponent<SnapPoint>();
        // ����� ����� ������ snapPosition � ��������, ���� �����
        snap.snapPosition = point.transform;
    }
}