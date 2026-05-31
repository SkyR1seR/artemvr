using UnityEngine;

/// <summary>
/// Триггер-крючок на нижнем конце динамометра.
/// Живёт на дочернем объекте с trigger-коллайдером и пересылает
/// касания брусков родительскому <see cref="SpringDynamometer"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DynamometerHook : MonoBehaviour
{
    [Tooltip("Динамометр, которому принадлежит этот крючок.")]
    public SpringDynamometer dynamometer;

    void OnTriggerStay(Collider other)
    {
        if (dynamometer == null) return;

        var brusok = other.GetComponent<Brusok>();
        if (brusok != null)
            dynamometer.TryAttach(brusok);
    }
}
