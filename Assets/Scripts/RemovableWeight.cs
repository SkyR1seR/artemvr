using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RemovableWeight : MonoBehaviour
{
    private SnapPoint currentSnap;
    private UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable grab;

    void Start()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (currentSnap != null)
            currentSnap.Release(gameObject);
    }

    public void SetSnapPoint(SnapPoint point)
    {
        currentSnap = point;
    }
}
