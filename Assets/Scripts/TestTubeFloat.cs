using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Пробирка-поплавок с пробкой. Управляется SteamVR (Interactable + Throwable).
/// В пробирку добавляется песок (<see cref="AddSand"/> вызывает SandScoop),
/// масса растёт, и тело перестаёт плавать. Визуал песка внутри пробирки
/// масштабируется по количеству, на метке показывается общая масса в граммах.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
public class TestTubeFloat : MonoBehaviour
{
    [Header("Массы")]
    [Tooltip("Масса пустой пробирки с пробкой, кг.")]
    public float emptyMassKg = 0.018f;
    [Tooltip("Максимум песка, кг.")]
    public float maxSandMassKg = 0.060f;

    [Header("Геометрия (для визуала песка)")]
    public float bodyRadius = 0.0125f;
    public float bodyLength = 0.12f;

    [Header("Ссылки")]
    [Tooltip("Цилиндр-песок внутри пробирки.")]
    public Transform sandFill;
    [Tooltip("Метка с массой.")]
    public TextMeshPro massLabel;

    public float SandMassKg  { get; private set; }
    public float TotalMassKg => emptyMassKg + SandMassKg;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        ApplyMass();
    }

    void Start()
    {
        UpdateSandVisual();
        UpdateLabel();
    }

    /// <summary>Добавить (или убрать при отрицательном) песок. Вызывается SandScoop.</summary>
    public void AddSand(float deltaKg)
    {
        SandMassKg = Mathf.Clamp(SandMassKg + deltaKg, 0f, maxSandMassKg);
        ApplyMass();
        UpdateSandVisual();
        UpdateLabel();
    }

    private void ApplyMass()
    {
        if (_rb != null) _rb.mass = TotalMassKg;
    }

    private void UpdateSandVisual()
    {
        if (sandFill == null) return;
        float frac = maxSandMassKg > 0f ? SandMassKg / maxSandMassKg : 0f;
        float fillH = frac * bodyLength * 0.8f;
        sandFill.localScale    = new Vector3(bodyRadius * 2f * 0.85f,
                                             Mathf.Max(0.0001f, fillH * 0.5f),
                                             bodyRadius * 2f * 0.85f);
        sandFill.localPosition = new Vector3(0f, -bodyLength * 0.5f + fillH * 0.5f + 0.002f, 0f);
        sandFill.gameObject.SetActive(SandMassKg > 0f);
    }

    private void UpdateLabel()
    {
        if (massLabel != null)
            massLabel.text = $"{Mathf.RoundToInt(TotalMassKg * 1000f)} г";
    }
}
