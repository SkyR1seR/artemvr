using UnityEngine;
using TMPro;

/// <summary>
/// Контроллер лабораторной работы №6: определение КПД при подъёме тела
/// по наклонной плоскости.
///
/// η = A_полезн / A_полн · 100% = (m · g · h) / (F · S) · 100%, где
///   m — масса бруска, g — ускорение свободного падения,
///   h — набранная высота, F — сила тяги (динамометр), S — путь по доске.
///
/// Активный брусок и сила берутся у <see cref="SpringDynamometer"/>; отсчёт
/// пути и высоты начинается с момента, когда брусок цепляют на крючок.
/// </summary>
public class InclinedPlaneEfficiency : MonoBehaviour
{
    [Header("Приборы")]
    public SpringDynamometer dynamometer;  // динамометр (источник силы и активного бруска)
    public MeasuringTape measuringTape;    // измерительная лента
    public Transform inclinedPlane;        // доска (для справки об угле наклона)

    [Header("UI (World Space)")]
    public TextMeshProUGUI angleText;      // текущий угол наклона α
    public TextMeshProUGUI massText;
    public TextMeshProUGUI forceText;
    public TextMeshProUGUI distanceText;   // путь S вдоль доски
    public TextMeshProUGUI heightText;     // высота h
    public TextMeshProUGUI efficiencyText; // КПД
    public TextMeshProUGUI workText;       // полезная / полная работа
    public TextMeshProUGUI hintText;       // подсказка

    [Header("Параметры")]
    public float g = 9.81f;

    private Brusok  _activeBlock;
    private Vector3 _startPosition;
    private float   _maxHeight;
    private float   _maxDistance;

    void Update()
    {
        // Текущий угол наклона доски (показываем всегда, даже без бруска).
        if (angleText != null && inclinedPlane != null)
        {
            // Локальная ось +X доски направлена вдоль склона; её Y-компонента = sin(α).
            float a = Mathf.Asin(Mathf.Clamp(inclinedPlane.right.y, -1f, 1f)) * Mathf.Rad2Deg;
            angleText.text = $"Угол:   α = {a:F0} °";
        }

        Brusok current = dynamometer != null ? dynamometer.AttachedBlock : null;

        // Брусок прицепили/сменили — начинаем новый отсчёт.
        if (current != _activeBlock)
        {
            _activeBlock = current;
            if (_activeBlock != null)
            {
                _startPosition = _activeBlock.transform.position;
                _maxHeight   = 0f;
                _maxDistance = 0f;
            }
        }

        if (_activeBlock == null)
        {
            ShowIdle();
            return;
        }

        Vector3 pos = _activeBlock.transform.position;

        // Путь по доске = смещение от точки старта (брусок движется вдоль плоскости).
        float distance = Vector3.Distance(_startPosition, pos);
        _maxDistance = Mathf.Max(_maxDistance, distance);

        // Набранная высота — прирост по вертикали.
        float height = Mathf.Max(0f, pos.y - _startPosition.y);
        _maxHeight = Mathf.Max(_maxHeight, height);

        if (measuringTape != null)
            measuringTape.ShowDistance(_maxDistance);

        float mass  = _activeBlock.massKg;
        float force = dynamometer.MeasuredForce;

        float usefulWork = mass * g * _maxHeight;   // A_полезн = mgh
        float totalWork  = force * _maxDistance;    // A_полн   = F·S

        if (massText)     massText.text     = $"Масса:  m = {mass:F2} кг";
        if (forceText)    forceText.text    = $"Сила:   F = {force:F2} Н";
        if (distanceText) distanceText.text = $"Путь:   S = {_maxDistance:F2} м";
        if (heightText)   heightText.text   = $"Высота: h = {_maxHeight:F2} м";
        if (workText)
            workText.text = $"A_пол = {usefulWork:F2} Дж\nA_зат = {totalWork:F2} Дж";

        if (efficiencyText)
        {
            if (totalWork > 0.0001f && _maxDistance > 0.01f)
            {
                float efficiency = Mathf.Clamp(usefulWork / totalWork * 100f, 0f, 100f);
                efficiencyText.text  = $"η = {efficiency:F1} %";
                efficiencyText.color = Color.green;
            }
            else
            {
                efficiencyText.text  = "η = — %";
                efficiencyText.color = new Color(0.8f, 0.8f, 0.8f);
            }
        }

        if (hintText)
            hintText.text = "Тяните брусок динамометром вверх по доске.";
    }

    private void ShowIdle()
    {
        if (massText)        massText.text        = "Масса:  m = — кг";
        if (forceText)       forceText.text       = "Сила:   F = — Н";
        if (distanceText)    distanceText.text    = "Путь:   S = — м";
        if (heightText)      heightText.text      = "Высота: h = — м";
        if (workText)        workText.text        = "A_пол = — Дж\nA_зат = — Дж";
        if (efficiencyText) { efficiencyText.text = "η = — %"; efficiencyText.color = new Color(0.8f, 0.8f, 0.8f); }
        if (hintText)        hintText.text        = "Возьмите динамометр и прицепите брусок крючком.";
    }

    /// <summary>Сброс отсчёта (для кнопки UI).</summary>
    public void ResetMeasurement()
    {
        if (_activeBlock != null)
            _startPosition = _activeBlock.transform.position;
        _maxHeight   = 0f;
        _maxDistance = 0f;
    }
}
