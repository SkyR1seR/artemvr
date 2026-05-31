using UnityEngine;

/// <summary>
/// Держит муфту и лапку штатива на верхнем конце наклонной доски, когда
/// студент меняет угол наклона ручкой-регулятором (CircularDrive на доске).
///
/// Муфта скользит по вертикальному стержню на высоту верхнего края доски,
/// а лапка тянется от муфты к этому краю — как у настоящего штатива.
/// </summary>
public class InclineClampFollower : MonoBehaviour
{
    [Tooltip("Шарнир доски (нижний край). Локальная ось +X направлена вдоль склона.")]
    public Transform boardPivot;
    [Tooltip("Длина доски, м.")]
    public float boardLength = 1.0f;

    [Tooltip("Вертикальный стержень штатива (для оси X/Z).")]
    public Transform rod;
    [Tooltip("Муфта — скользит по стержню.")]
    public Transform coupling;
    [Tooltip("Лапка — держатель от муфты к доске (цилиндр, длина по локальной оси Y).")]
    public Transform clampArm;
    [Tooltip("Головка лапки у края доски.")]
    public Transform clampHead;

    [Tooltip("Радиус лапки, м.")]
    public float armRadius = 0.025f;

    void LateUpdate()
    {
        if (boardPivot == null) return;

        // Верхний конец доски в мировых координатах.
        Vector3 upper = boardPivot.position + boardPivot.right * boardLength;

        // Муфта едет по стержню на высоту верхнего конца.
        Vector3 rodPos = rod != null ? rod.position : transform.position;
        Vector3 couplingPos = new Vector3(rodPos.x, upper.y, rodPos.z);
        if (coupling != null)
            coupling.position = couplingPos;

        if (clampHead != null)
            clampHead.position = upper;

        // Лапка соединяет муфту и край доски.
        if (clampArm != null)
        {
            Vector3 a = couplingPos;
            Vector3 b = upper;
            Vector3 axis = b - a;
            float len = axis.magnitude;

            clampArm.position = (a + b) * 0.5f;
            if (len > 0.0001f)
            {
                clampArm.up         = axis / len;           // локальная Y вдоль лапки
                clampArm.localScale = new Vector3(armRadius, len * 0.5f, armRadius);
            }
        }
    }
}
