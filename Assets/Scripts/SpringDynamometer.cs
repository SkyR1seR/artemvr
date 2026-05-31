using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Пружинный динамометр для лабораторной работы «КПД наклонной плоскости».
///
/// Студент берёт динамометр SteamVR-рукой (Interactable + Throwable) и подносит
/// крючок к бруску — брусок прицепляется на пружину (SpringJoint). Затем брусок
/// тянут вверх по наклонной плоскости: пружина растягивается, а величина силы
/// F = k · Δx отображается на шкале и передаётся в <see cref="ForceMeter"/>.
///
/// Физика остаётся честной: SpringJoint реально тянет Rigidbody бруска, поэтому
/// сила натяжения совпадает с тем, что показывает прибор.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
public class SpringDynamometer : MonoBehaviour
{
    [Header("Пружина")]
    [Tooltip("Жёсткость пружины k (Н/м). Чем выше, тем «жёстче» прибор.")]
    public float springConstant = 150f;
    [Tooltip("Затухание пружины — гасит колебания.")]
    public float springDamper = 6f;

    [Header("Геометрия")]
    [Tooltip("Точка крючка (нижний конец прибора). Если не задана — используется этот объект.")]
    public Transform hook;

    [Header("Индикация")]
    [Tooltip("Прибор для передачи измеренной силы и числового табло.")]
    public ForceMeter forceMeter;
    [Tooltip("Стрелка-указатель (поворачивается вокруг локальной оси Z). Необязательно.")]
    public Transform needle;
    [Tooltip("Сила, соответствующая максимальному отклонению стрелки (Н).")]
    public float fullScaleForce = 10f;
    [Tooltip("Углы стрелки при 0 Н и при fullScaleForce (градусы).")]
    public float needleAngleAtZero = 90f;
    public float needleAngleAtFull = -90f;
    [Tooltip("Линия-«нить» от крючка к бруску (необязательно).")]
    public LineRenderer tether;

    private Rigidbody _rb;
    private Brusok _attached;
    private SpringJoint _joint;
    private float _measuredForce;
    private bool _isHeld;

    public bool IsBeingHeld     => _isHeld;
    public Brusok AttachedBlock => _attached;
    public float MeasuredForce  => _measuredForce;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (hook == null) hook = transform;
    }

    // ─── SteamVR SendMessage hooks ───────────────────────────────────────────

    private void OnAttachedToHand(Hand hand)   => _isHeld = true;
    private void OnDetachedFromHand(Hand hand)  => _isHeld = false;

    // ─── Захват бруска крючком ───────────────────────────────────────────────

    /// <summary>Пытается прицепить брусок. Вызывается из DynamometerHook.</summary>
    public void TryAttach(Brusok brusok)
    {
        // Цепляем только когда прибор в руке, брусок свободен и ещё ничего не висит.
        if (!_isHeld || _attached != null || brusok == null) return;
        if (brusok.IsBeingHeld || brusok.IsAttached) return;

        var brusokRb = brusok.GetComponent<Rigidbody>();
        if (brusokRb == null) return;

        _joint = gameObject.AddComponent<SpringJoint>();
        _joint.connectedBody             = brusokRb;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.anchor                    = transform.InverseTransformPoint(hook.position);
        _joint.connectedAnchor           = Vector3.zero;   // центр бруска
        _joint.minDistance               = 0f;
        _joint.maxDistance               = 0f;             // длина покоя ≈ 0 → F = k·Δx
        _joint.spring                    = springConstant;
        _joint.damper                    = springDamper;
        _joint.enableCollision           = false;

        _attached = brusok;
        brusok.OnHooked(this);
    }

    /// <summary>Снимает брусок с крючка. Вызывается при захвате бруска рукой.</summary>
    public void Detach()
    {
        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
        }
        if (_attached != null)
        {
            _attached.OnUnhooked();
            _attached = null;
        }
        _measuredForce = 0f;
        if (forceMeter != null) forceMeter.CurrentForce = 0f;
        UpdateNeedle(0f);
        if (tether != null) tether.enabled = false;
    }

    // ─── Измерение силы ──────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (_attached == null) return;

        // F = k · Δx, где Δx — растяжение пружины (расстояние крючок→брусок).
        Vector3 hookPos   = hook.position;
        Vector3 blockPos  = _attached.transform.position;
        float   extension = Vector3.Distance(hookPos, blockPos);
        float   force     = springConstant * extension;

        // Сглаживаем показания, чтобы стрелка не дрожала.
        _measuredForce = Mathf.Lerp(_measuredForce, force, 0.25f);

        if (forceMeter != null) forceMeter.CurrentForce = _measuredForce;
        UpdateNeedle(_measuredForce);
        UpdateTether(hookPos, blockPos);
    }

    private void UpdateNeedle(float force)
    {
        if (needle == null) return;
        float t = fullScaleForce > 0f ? Mathf.Clamp01(force / fullScaleForce) : 0f;
        float angle = Mathf.Lerp(needleAngleAtZero, needleAngleAtFull, t);
        var e = needle.localEulerAngles;
        needle.localEulerAngles = new Vector3(e.x, e.y, angle);
    }

    private void UpdateTether(Vector3 a, Vector3 b)
    {
        if (tether == null) return;
        tether.enabled = true;
        tether.positionCount = 2;
        tether.SetPosition(0, a);
        tether.SetPosition(1, b);
    }
}
