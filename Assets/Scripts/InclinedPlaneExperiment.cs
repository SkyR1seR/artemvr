using UnityEngine;
using TMPro;

public class InclinedPlaneExperiment : MonoBehaviour
{
    [Header("Объекты сцены")]
    public GameObject inclinedPlane;    // наклонная плоскость
    public GameObject block;            // брусок
    public ForceMeter forceMeter;       // динамометр
    public MeasuringTape measuringTape; // рулетка

    [Header("UI")]
    public TextMeshProUGUI efficiencyText;  // текст КПД
    public TextMeshProUGUI forceText;       // текущая сила
    public TextMeshProUGUI distanceText;    // пройденное расстояние
    public TextMeshProUGUI heightText;      // высота подъёма

    [Header("Настройки")]
    public float blockMass = 1f;        // масса бруска (кг)
    public float g = 9.81f;             // ускорение св. падения

    private Vector3 startPosition;
    private float maxHeight = 0f;

    void Start()
    {
        if (block != null)
            startPosition = block.transform.position;
        if (forceMeter != null)
            forceMeter.onForceChanged.AddListener(OnForceChanged);
    }

    void Update()
    {
        if (block == null || inclinedPlane == null) return;

        // Вычисляем пройденное расстояние по наклонной плоскости
        Vector3 currentPos = block.transform.position;
        float distanceTraveled = Vector3.Distance(startPosition, currentPos);
        if (measuringTape != null)
            measuringTape.ShowDistance(distanceTraveled);

        // Вычисляем текущую высоту подъёма (относительно начальной точки)
        float currentHeight = Mathf.Max(0, currentPos.y - startPosition.y);
        maxHeight = Mathf.Max(maxHeight, currentHeight);

        // Обновляем UI
        if (distanceText != null)
            distanceText.text = $"Путь: {distanceTraveled:F2} м";
        if (heightText != null)
            heightText.text = $"Высота: {currentHeight:F2} м";

        // Полезная работа = m*g*h (текущая высота)
        float usefulWork = blockMass * g * currentHeight;
        // Полная работа = F * S (сила * путь). Сила берётся из динамометра.
        float currentForce = forceMeter != null ? forceMeter.CurrentForce : 0f;
        float totalWork = currentForce * distanceTraveled;

        float efficiency = totalWork > 0 ? (usefulWork / totalWork) * 100f : 0f;
        if (efficiencyText != null)
            efficiencyText.text = $"КПД: {efficiency:F1}%";

        if (forceText != null)
            forceText.text = $"Сила: {currentForce:F2} Н";
    }

    void OnForceChanged(float force)
    {
        // Можно добавить эффекты (вибрация, звук)
    }

    public void ResetExperiment()
    {
        if (block != null)
            block.transform.position = startPosition;
        maxHeight = 0f;
    }
}