using UnityEngine;


public class SnapPoint : MonoBehaviour
{
    public Transform snapPosition;   // ���� ������ ����������� ����
    private GameObject currentObject;

    void OnTriggerEnter(Collider other)
    {
        if (currentObject != null) return;
        UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable grab = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (grab != null && !grab.isSelected)
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                other.transform.SetParent(snapPosition);
                other.transform.localPosition = Vector3.zero;
                other.transform.localRotation = Quaternion.identity;
                currentObject = other.gameObject;

                // �������� �����, ��� �� ���������
                RemovableWeight weight = other.GetComponent<RemovableWeight>();
                if (weight != null) weight.SetSnapPoint(this);
            }
        }
    }

    public void Release(GameObject obj)
    {
        if (currentObject == obj)
        {
            obj.transform.SetParent(null);
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            currentObject = null;
        }
    }
}
