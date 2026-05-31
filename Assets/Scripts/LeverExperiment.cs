using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Контроллер лабораторной работы по рычагу.
/// Считает моменты сил и условие равновесия, выводит на UI-панель.
/// Формула: рычаг в равновесии когда M₁ = M₂, т.е. F₁·l₁ = F₂·l₂
/// </summary>
public class LeverExperiment : MonoBehaviour
{
    [Header("Слоты левого плеча (от оси к краю)")]
    public List<LeverSlot> leftSlots = new List<LeverSlot>();

    [Header("Слоты правого плеча (от оси к краю)")]
    public List<LeverSlot> rightSlots = new List<LeverSlot>();

    [Header("UI")]
    public TextMeshProUGUI leftMomentText;
    public TextMeshProUGUI rightMomentText;
    public TextMeshProUGUI balanceStatusText;
    public TextMeshProUGUI formulaText;
    public TextMeshProUGUI leftWeightsText;
    public TextMeshProUGUI rightWeightsText;

    private const float G = 9.81f;
    private const float BalanceThreshold = 0.005f;  // Н·м, порог "равновесие"

    void Update()
    {
        float mLeft  = CalculateMoment(leftSlots);
        float mRight = CalculateMoment(rightSlots);

        if (leftMomentText)  leftMomentText.text  = $"M₁ = {mLeft:F3} Н·м";
        if (rightMomentText) rightMomentText.text = $"M₂ = {mRight:F3} Н·м";

        UpdateBalanceStatus(mLeft, mRight);
        UpdateWeightsInfo(leftSlots,  leftWeightsText,  "Левое плечо");
        UpdateWeightsInfo(rightSlots, rightWeightsText, "Правое плечо");
        UpdateFormula(mLeft, mRight);
    }

    private float CalculateMoment(List<LeverSlot> slots)
    {
        float moment = 0f;
        foreach (var slot in slots)
            if (slot.IsOccupied && slot.SnappedWeight != null)
                moment += slot.SnappedWeight.massKg * G * slot.distanceFromPivot;
        return moment;
    }

    private void UpdateBalanceStatus(float mLeft, float mRight)
    {
        if (balanceStatusText == null) return;
        float diff = mLeft - mRight;
        if (Mathf.Abs(diff) <= BalanceThreshold)
        {
            balanceStatusText.text  = "⚖  РАВНОВЕСИЕ";
            balanceStatusText.color = Color.green;
        }
        else if (diff > 0)
        {
            balanceStatusText.text  = "←  Перевешивает левая сторона";
            balanceStatusText.color = new Color(1f, 0.55f, 0f);
        }
        else
        {
            balanceStatusText.text  = "→  Перевешивает правая сторона";
            balanceStatusText.color = new Color(1f, 0.55f, 0f);
        }
    }

    private void UpdateWeightsInfo(List<LeverSlot> slots, TextMeshProUGUI text, string header)
    {
        if (text == null) return;
        var sb = new System.Text.StringBuilder(header + ":\n");
        bool any = false;
        foreach (var slot in slots)
        {
            if (!slot.IsOccupied || slot.SnappedWeight == null) continue;
            int grams = Mathf.RoundToInt(slot.SnappedWeight.massKg * 1000f);
            string massStr = grams >= 1000 ? $"{slot.SnappedWeight.massKg:F1} кг" : $"{grams} г";
            sb.AppendLine($"  {massStr} на {slot.distanceFromPivot:F2} м");
            any = true;
        }
        if (!any) sb.AppendLine("  — грузов нет");
        text.text = sb.ToString();
    }

    private void UpdateFormula(float mLeft, float mRight)
    {
        if (formulaText == null) return;

        string lSide = BuildSideExpression(leftSlots);
        string rSide = BuildSideExpression(rightSlots);
        bool balanced = Mathf.Abs(mLeft - mRight) <= BalanceThreshold;

        string eq = balanced ? "=" : (mLeft > mRight ? ">" : "<");
        formulaText.text = $"F₁·l₁  {eq}  F₂·l₂\n{lSide}  {eq}  {rSide}";
    }

    private string BuildSideExpression(List<LeverSlot> slots)
    {
        var terms = new List<string>();
        foreach (var slot in slots)
        {
            if (!slot.IsOccupied || slot.SnappedWeight == null) continue;
            float F = slot.SnappedWeight.massKg * G;
            float l = slot.distanceFromPivot;
            terms.Add($"{F:F2}·{l:F2}");
        }
        return terms.Count > 0 ? string.Join(" + ", terms) : "0";
    }
}
