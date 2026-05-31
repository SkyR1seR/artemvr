using UnityEngine;

/// <summary>
/// Архимедова сила для тела-цилиндра (пробирки-поплавка), погружённого в
/// <see cref="WaterColumn"/>. Считает погружённую высоту относительно уровня
/// воды, прикладывает выталкивающую силу F = ρ·g·V_погр и сообщает цилиндру
/// вытесненный объём (чтобы уровень поднимался).
///
/// Тело держится вертикально (заморозка вращения в Rigidbody) и мягко
/// центрируется по оси мензурки, пока находится внутри неё.
/// Пока тело в руке (Throwable → isKinematic), силы не прикладываются,
/// но вытеснение по-прежнему считается (уровень реагирует на погружение).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    public WaterColumn water;

    [Header("Геометрия тела (вертикальный цилиндр)")]
    public float bodyRadius = 0.0125f;
    public float bodyLength = 0.12f;

    [Header("Физика")]
    public float fluidDensity = 1000f;  // вода, кг/м³
    public float gravity      = 9.81f;

    [Header("Демпфирование (1/с, не зависит от массы)")]
    [Tooltip("Гашение вертикальных колебаний при всплытии/погружении.")]
    public float verticalDamping  = 6f;
    [Tooltip("Общее гашение скорости в воде (масштабируется глубиной погружения).")]
    public float submergedDamping = 4f;
    [Tooltip("Возврат к оси мензурки (жёсткость пружины, ω²).")]
    public float centeringStrength = 25f;
    [Tooltip("Гашение горизонтальных колебаний при центрировании.")]
    public float centeringDamping = 6f;

    private Rigidbody _rb;
    public float SubmergedVolume { get; private set; }

    void Awake() => _rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (water == null) return;

        Vector3 pos = transform.position;
        float bottom = pos.y - bodyLength * 0.5f;

        float dx = pos.x - water.transform.position.x;
        float dz = pos.z - water.transform.position.z;
        float distXZ = Mathf.Sqrt(dx * dx + dz * dz);
        bool insideColumn = distXZ < water.innerRadius * 1.6f;

        float submergedH = insideColumn
            ? Mathf.Clamp(water.SurfaceY - bottom, 0f, bodyLength)
            : 0f;

        SubmergedVolume = Mathf.PI * bodyRadius * bodyRadius * submergedH;
        water.SetDisplacedVolume(SubmergedVolume);

        if (_rb.isKinematic) return;   // в руке — без сил

        if (submergedH > 0f)
        {
            float frac = submergedH / bodyLength;   // доля погружения 0..1

            // Выталкивающая сила (реальная, зависит от массы → ForceMode.Force).
            float buoyForce = fluidDensity * gravity * SubmergedVolume;
            _rb.AddForce(Vector3.up * buoyForce, ForceMode.Force);

            // Демпфирование как ускорение — не зависит от массы (масса меняется
            // при досыпании песка), поэтому гашение остаётся стабильным.
            _rb.AddForce(Vector3.up * (-_rb.velocity.y * verticalDamping), ForceMode.Acceleration);
            _rb.AddForce(-_rb.velocity * (submergedDamping * frac), ForceMode.Acceleration);
        }

        // Центрирование по оси мензурки, пока тело ниже верхнего края.
        if (insideColumn && pos.y < water.bottomY + 0.30f)
        {
            Vector3 toAxis = new Vector3(-dx, 0f, -dz) * centeringStrength;
            Vector3 damp   = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z) * centeringDamping;
            _rb.AddForce(toAxis - damp, ForceMode.Acceleration);
        }
    }
}
