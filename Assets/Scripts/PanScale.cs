using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Простые весы с чашей: суммируют массу Rigidbody-тел, находящихся в
/// триггере над чашей, и показывают её в граммах. Показания живые —
/// если масса тела меняется (досыпали песок), значение обновляется.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PanScale : MonoBehaviour
{
    [Tooltip("Табло массы (мировой TextMeshPro).")]
    public TextMeshPro display;
    [Tooltip("Округление показаний, г.")]
    public float gramsRounding = 1f;

    private readonly HashSet<Rigidbody> _onPan = new HashSet<Rigidbody>();

    public float TotalMassKg
    {
        get
        {
            _onPan.RemoveWhere(r => r == null);
            float sum = 0f;
            foreach (var rb in _onPan) sum += rb.mass;
            return sum;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null) _onPan.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb != null) _onPan.Remove(rb);
    }

    void Update()
    {
        if (display == null) return;
        float grams = TotalMassKg * 1000f;
        if (gramsRounding > 0f) grams = Mathf.Round(grams / gramsRounding) * gramsRounding;
        display.text = $"{grams:F0} г";
    }
}
