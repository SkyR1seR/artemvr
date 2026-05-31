using UnityEngine;

/// <summary>
/// Слот для крепления груза на рычаге.
/// Груз притягивается через FixedJoint к Rigidbody рычага —
/// это сохраняет точную физическую симуляцию (груз вносит реальный момент силы).
/// Совместим с SteamVR Interaction System: LeverWeight.IsBeingHeld отражает
/// состояние захвата SteamVR-рукой.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LeverSlot : MonoBehaviour
{
    [Tooltip("Расстояние от оси вращения в метрах (мировые единицы). Используется для отображения момента силы.")]
    public float distanceFromPivot = 0.6f;

    [Tooltip("Rigidbody плеча рычага. Устанавливается при настройке сцены.")]
    public Rigidbody armRigidbody;

    private LeverWeight _snappedWeight;
    private FixedJoint  _joint;
    private Renderer    _markerRenderer;
    private Collider    _armCollider;

    private static readonly Color ColorEmpty    = new(0.15f, 0.85f, 0.15f);
    private static readonly Color ColorOccupied = new(0.90f, 0.25f, 0.10f);

    public bool IsOccupied           => _snappedWeight != null;
    public LeverWeight SnappedWeight => _snappedWeight;

    void Start()
    {
        _markerRenderer = GetComponentInChildren<Renderer>();
        if (armRigidbody != null)
            _armCollider = armRigidbody.GetComponent<Collider>();
        SetMarkerColor(ColorEmpty);
    }

    void OnTriggerStay(Collider other)
    {
        if (IsOccupied || armRigidbody == null) return;

        var weight = other.GetComponent<LeverWeight>();
        if (weight == null || weight.IsSnapped) return;

        // Не снаплять, пока SteamVR-рука держит груз
        if (weight.IsBeingHeld) return;

        SnapWeight(weight);
    }

    private void SnapWeight(LeverWeight weight)
    {
        var rb = weight.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;

        // Останавливаем груз и ставим на позицию слота
        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Смещаем груз чуть выше поверхности плеча
        Vector3 snapUp = armRigidbody.transform.up;
        float   offset = armRigidbody.transform.lossyScale.y * 0.5f
                       + weight.transform.lossyScale.y * 0.5f;
        weight.transform.SetPositionAndRotation(transform.position + snapUp * offset, Quaternion.identity);

        // Отключаем столкновение груза с плечом, чтобы не было дрожания
        var weightCollider = weight.GetComponent<Collider>();
        if (weightCollider != null && _armCollider != null)
            Physics.IgnoreCollision(weightCollider, _armCollider, true);

        // FixedJoint крепит груз к плечу — физический движок сам считает моменты
        _joint = weight.gameObject.AddComponent<FixedJoint>();
        _joint.connectedBody = armRigidbody;
        _joint.breakForce    = Mathf.Infinity;
        _joint.breakTorque   = Mathf.Infinity;

        _snappedWeight = weight;
        weight.OnSnapped(this);
        SetMarkerColor(ColorOccupied);
    }

    public void ReleaseWeight()
    {
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }

        if (_snappedWeight != null)
        {
            var weightCollider = _snappedWeight.GetComponent<Collider>();
            if (weightCollider != null && _armCollider != null)
                Physics.IgnoreCollision(weightCollider, _armCollider, false);

            _snappedWeight.OnReleased();
            _snappedWeight = null;
        }

        SetMarkerColor(ColorEmpty);
    }

    private void SetMarkerColor(Color c)
    {
        if (_markerRenderer != null)
            _markerRenderer.material.color = c;
    }
}
