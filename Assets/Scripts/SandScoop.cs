using UnityEngine;

/// <summary>
/// Совок для песка. Триггер на кончике совка:
///  • касание <see cref="SandPile"/> — совок наполняется песком;
///  • касание пробирки (<see cref="TestTubeFloat"/>) — песок высыпается в неё,
///    масса пробирки увеличивается на <see cref="sandPerScoop"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SandScoop : MonoBehaviour
{
    [Tooltip("Сколько песка добавляется за один совок, кг (6 г).")]
    public float sandPerScoop = 0.006f;

    [Tooltip("Визуал песка в совке (включается, когда совок полон).")]
    public Renderer sandInScoop;

    private bool  _hasSand;
    private float _cooldown;

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        if (sandInScoop != null) sandInScoop.enabled = _hasSand;
    }

    void OnTriggerEnter(Collider other)
    {
        // Набрать песок из кучи.
        if (!_hasSand && other.GetComponentInParent<SandPile>() != null)
        {
            _hasSand = true;
            return;
        }

        // Высыпать в пробирку.
        var tube = other.GetComponentInParent<TestTubeFloat>();
        if (_hasSand && tube != null && _cooldown <= 0f)
        {
            tube.AddSand(sandPerScoop);
            _hasSand  = false;
            _cooldown = 0.5f;
        }
    }
}
