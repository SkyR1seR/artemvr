using UnityEngine;

/// <summary>
/// Вода в измерительном цилиндре (мензурке) для лабораторной работы №2
/// «Условия плавания тела». Хранит базовый объём воды V₁ и поднимает уровень
/// при погружении тела на величину вытесненного объёма (закон Архимеда).
///
/// Уровень воды: y = bottomY + (V₁ + Vвыт) / S, где S = π·r² — площадь сечения.
/// Видимый меш воды масштабируется под текущий уровень.
/// </summary>
public class WaterColumn : MonoBehaviour
{
    [Header("Геометрия мензурки (мировые единицы)")]
    [Tooltip("Внутренний радиус цилиндра, м.")]
    public float innerRadius = 0.03f;
    [Tooltip("Мировой Y дна воды.")]
    public float bottomY = 0f;
    [Tooltip("V₁ — объём воды без тела, м³ (400 мл = 4e-4).")]
    public float baseVolume = 4.0e-4f;

    [Header("Визуал")]
    [Tooltip("Цилиндр-вода (масштабируется по высоте уровня).")]
    public Transform waterMesh;

    public float CrossSection   => Mathf.PI * innerRadius * innerRadius;
    public float SurfaceY        { get; private set; }
    public float DisplacedVolume { get; private set; }
    public float BaseVolume      => baseVolume;
    public float CurrentVolume   => baseVolume + DisplacedVolume;

    void Start() => SetDisplacedVolume(0f);

    /// <summary>Задать вытесненный объём (вызывается Buoyancy) и обновить уровень.</summary>
    public void SetDisplacedVolume(float volume)
    {
        DisplacedVolume = Mathf.Max(0f, volume);
        SurfaceY = bottomY + CurrentVolume / CrossSection;
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (waterMesh == null) return;
        float h = Mathf.Max(0f, SurfaceY - bottomY);
        // Примитив-цилиндр Unity имеет высоту 2 ед. → scale.y = h/2.
        waterMesh.position   = new Vector3(transform.position.x, bottomY + h * 0.5f, transform.position.z);
        waterMesh.localScale = new Vector3(innerRadius * 2f * 0.97f, h * 0.5f, innerRadius * 2f * 0.97f);
    }
}
