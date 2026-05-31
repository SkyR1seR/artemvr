using UnityEngine;
using TMPro;

/// <summary>
/// Контроллер лабораторной работы №2: «Выяснение условий плавания тела».
///
/// Считывает у мензурки объёмы V₁ (вода) и V₂ (вода + пробирка), вычисляет
/// выталкивающую силу F_A = ρ·g·(V₂−V₁) и сравнивает её с силой тяжести
/// F_тяж = m·g пробирки. Выводит таблицу величин и вывод: тело плавает,
/// «висит» в толще воды или тонет.
///
/// Условие плавания определяется по массе: тело может плавать, пока его
/// средняя плотность меньше плотности воды (масса меньше массы вытесненной
/// воды при полном погружении m_полн = ρ·V_тела).
/// </summary>
public class BuoyancyExperiment : MonoBehaviour
{
    [Header("Приборы")]
    public WaterColumn water;
    public TestTubeFloat tube;
    public PanScale balance;

    [Header("Константы")]
    public float fluidDensity = 1000f;
    public float g = 9.81f;

    [Header("UI (World Space)")]
    public TextMeshProUGUI v1Text;
    public TextMeshProUGUI v2Text;
    public TextMeshProUGUI buoyText;     // F_A
    public TextMeshProUGUI massText;     // m пробирки
    public TextMeshProUGUI weightText;   // F_тяж
    public TextMeshProUGUI balanceText;  // показания весов
    public TextMeshProUGUI verdictText;  // вывод
    public TextMeshProUGUI hintText;

    void Update()
    {
        if (water != null)
        {
            float fA = fluidDensity * g * water.DisplacedVolume;
            if (v1Text)   v1Text.text   = $"V₁ = {water.BaseVolume    * 1e6f:F0} мл";
            if (v2Text)   v2Text.text   = $"V₂ = {water.CurrentVolume * 1e6f:F0} мл";
            if (buoyText) buoyText.text = $"F_A = {fA:F2} Н";
        }

        if (tube != null)
        {
            float m = tube.TotalMassKg;
            if (massText)   massText.text   = $"m = {Mathf.RoundToInt(m * 1000f)} г";
            if (weightText) weightText.text = $"F_тяж = {m * g:F2} Н";
            UpdateVerdict(m);
        }

        if (balance != null && balanceText != null)
            balanceText.text = $"Весы: {Mathf.Round(balance.TotalMassKg * 1000f)} г";
    }

    private void UpdateVerdict(float m)
    {
        if (verdictText == null || tube == null) return;

        float tubeVolume = Mathf.PI * tube.bodyRadius * tube.bodyRadius * tube.bodyLength;
        float mFull = fluidDensity * tubeVolume;   // масса полного погружения

        if (m < mFull * 0.97f)
        {
            verdictText.text  = "Пробирка ПЛАВАЕТ\n(часть над водой)\nF_A > F_тяж";
            verdictText.color = new Color(0.4f, 0.9f, 0.4f);
        }
        else if (m <= mFull * 1.03f)
        {
            verdictText.text  = "Пробирка ВИСИТ\nв толще воды\nF_A ≈ F_тяж";
            verdictText.color = new Color(1f, 0.85f, 0.25f);
        }
        else
        {
            verdictText.text  = "Пробирка ТОНЕТ\nF_A < F_тяж";
            verdictText.color = new Color(1f, 0.45f, 0.35f);
        }
    }
}
