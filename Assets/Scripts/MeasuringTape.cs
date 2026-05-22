using UnityEngine;
using TMPro;

public class MeasuringTape : MonoBehaviour
{
    public TextMeshPro distanceLabel;
    public Transform pointA; // можно задать начальную точку (низ плоскости)
    public Transform pointB; // конечная точка (верх плоскости)

    void Start()
    {
        if (distanceLabel == null)
        {
            var go = new GameObject("DistanceLabel");
            go.transform.parent = transform;
            var text = go.AddComponent<TextMeshPro>();
            text.fontSize = 5;
            text.alignment = TextAlignmentOptions.Center;
            distanceLabel = text;
            distanceLabel.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
    }

    public void ShowDistance(float distance)
    {
        if (distanceLabel != null)
            distanceLabel.text = $"{distance:F2} м";
    }
}